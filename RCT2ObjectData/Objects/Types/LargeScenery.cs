﻿using RCT2ObjectData.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCT2ObjectData.Objects.Types {
	/**<summary>A large scenery object.</summary>*/
	public class LargeScenery : ObjectData {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The size of the header for this object type.</summary>*/
		public const uint HeaderSize = 0x1A;

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The object header.</summary>*/
		public LargeSceneryHeader Header;
		/**<summary>The 3D text data.</summary>*/
		public List<byte> Text3D;
		/**<summary>The list of tile headers in the object.</summary>*/
		public List<LargeSceneryTileHeader> Tiles;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the default object.</summary>*/
		public LargeScenery() : base() {
			Header	= new LargeSceneryHeader();
			Text3D	= new List<byte>();
			Tiles	= new List<LargeSceneryTileHeader>();
		}
		/**<summary>Constructs the default object.</summary>*/
		internal LargeScenery(ObjectDataHeader objectHeader, ChunkHeader chunkHeader)
			: base(objectHeader, chunkHeader) {
			Header	= new LargeSceneryHeader();
			Text3D	= new List<byte>();
			Tiles	= new List<LargeSceneryTileHeader>();
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties
		//--------------------------------
		#region Reading

		/**<summary>Gets the number of string table entries in the object.</summary>*/
		public override int NumStringTableEntries {
			get { return 1; }
		}
		/**<summary>Returns true if the object has a group info section.</summary>*/
		public override bool HasGroupInfo {
			get { return true; }
		}

		#endregion
		//--------------------------------
		#region Information

		/**<summary>Gets the subtype of the object.</summary>*/
		public override ObjectSubtypes Subtype {
			get {
				if (Header.Flags.HasFlag(LargeSceneryFlags.Text3D))
					return ObjectSubtypes.Text3D;
				if (Header.Flags.HasFlag(LargeSceneryFlags.TextScrolling))
					return ObjectSubtypes.TextScrolling;
				if (Header.Flags.HasFlag(LargeSceneryFlags.Photogenic))
					return ObjectSubtypes.Photogenic;
				return ObjectSubtypes.Basic;
			}
		}
		/**<summary>True if the object can be placed on a slope.</summary>*/
		public override bool CanSlope {
			get { return false; }
		}
		/**<summary>Gets the number of color remaps.</summary>*/
		public override int ColorRemaps {
			get { return (Header.Flags.HasFlag(LargeSceneryFlags.Remap2) ? 2 : (Header.Flags.HasFlag(LargeSceneryFlags.Remap1) ? 1 : 0)); }
		}
		/**<summary>Gets if the dialog view has color remaps.</summary>*/
		public override bool HasDialogColorRemaps {
			get { return true; }
		}
		/**<summary>Gets the number of frames in the animation.</summary>*/
		public override int AnimationFrames {
			get { return 1; }
		}

		#endregion
		//--------------------------------
		#endregion
		//=========== READING ============
		#region Reading

		/**<summary>Reads the object header.</summary>*/
		protected override void ReadHeader(BinaryReader reader) {
			Header.Read(reader);
		}
		/**<summary>Writes the object.</summary>*/
		protected override void WriteHeader(BinaryWriter writer) {
			Header.Write(writer);
		}
		/**<summary>Reads the object data optional.</summary>*/
		protected override void ReadOptional(BinaryReader reader) {
			// Read the 3D text
			if (Header.Flags.HasFlag(LargeSceneryFlags.Text3D)) {
				for (int i = 0; i < 0x40E; i++) {
					Text3D.Add(reader.ReadByte());
				}
			}
			// Read the tiles
			ushort flag = reader.ReadUInt16();
			while (flag != 0xFFFF) {
				reader.BaseStream.Position -= 2;
				LargeSceneryTileHeader tile = new LargeSceneryTileHeader();
				tile.Read(reader);
				Tiles.Add(tile);
				flag = reader.ReadUInt16();
			}
		}
		/**<summary>Writes the object data optional.</summary>*/
		protected override void WriteOptional(BinaryWriter writer) {
			// Write the 3D text
			if (Header.Flags.HasFlag(LargeSceneryFlags.Text3D)) {
				for (int i = 0; i < 0x40E; i++) {
					writer.Write(Text3D[i]);
				}
			}
			// Write the tiles
			for (int i = 0; i < Tiles.Count; i++) {
				Tiles[i].Write(writer);
			}
			writer.Write((ushort)0xFFFF);
		}
	
		#endregion
		//=========== DRAWING ============
		#region Drawing

		/**<summary>Constructs the default object.</summary>*/
		public override bool Draw(PaletteImage p, Point position, DrawSettings drawSettings) {
			try {
				LargeSceneryTileHeader[] tiles = new LargeSceneryTileHeader[Tiles.Count];
				Rectangle bounds = new Rectangle(100, 100, 0, 0);
				for (int i = 0; i < Tiles.Count; i++) {
					tiles[i] = Tiles[i];
					tiles[i].Row /= 32;
					tiles[i].Column /= 32;
					tiles[i].Index = i;
					switch (drawSettings.Rotation) {
					case 0: tiles[i].Depth = tiles[i].Row * 1.001f - tiles[i].Column; break;
					case 1: tiles[i].Depth = tiles[i].Row + tiles[i].Column * 1.001f; break;
					case 2: tiles[i].Depth = -tiles[i].Row * 1.001f - tiles[i].Column; break;
					case 3: tiles[i].Depth = -tiles[i].Row + tiles[i].Column * 1.001f; break;
					}
					if (tiles[i].Row < bounds.X)	bounds.X = tiles[i].Row;
					if (tiles[i].Column < bounds.Y)	bounds.Y = tiles[i].Column;
					if (tiles[i].Row >= bounds.Width)		bounds.Width = tiles[i].Row + 1;
					if (tiles[i].Column >= bounds.Height)	bounds.Height = tiles[i].Column + 1;
				}

				// Insertion sort the tile depths
				for (int i = 1; i < tiles.Length; i++) {
					LargeSceneryTileHeader tile = tiles[i];
					int j;

					for (j = i - 1; j >= 0; j--) {
						if (tile.Depth >= tiles[j].Depth)
							break;
						tiles[j + 1] = tiles[j];
					}
					tiles[j + 1] = tile;
				}

				// Get tht tile corner
				Point cornerPos = Point.Empty;
				switch (drawSettings.Rotation) {
				case 0: cornerPos = new Point(bounds.Width - 1, bounds.Y); break;
				case 1: cornerPos = new Point(bounds.Width - 1, bounds.Height - 1); break;
				case 2: cornerPos = new Point(bounds.X, bounds.Height - 1); break;
				case 3: cornerPos = new Point(bounds.X, bounds.Y); break;
				}

				// Draw each tile
				for (int i = 0; i < tiles.Length; i++) {
					int rot = (drawSettings.Rotation % 2 == 0 ? -1 :  1);
					Point point = new Point(
						(Math.Abs(tiles[i].Row - cornerPos.X) * rot + Math.Abs(tiles[i].Column - cornerPos.Y) * -rot),
						(Math.Abs(tiles[i].Row - cornerPos.X) * -1 + Math.Abs(tiles[i].Column - cornerPos.Y) * -1)
					);

					DrawFrame(p, new Point(
						position.X + point.X * 32,
						position.Y + point.Y * 16
					), drawSettings, 4 + tiles[i].Index * 4 + (drawSettings.Rotation + 3) % 4);
				}
			}
			catch (IndexOutOfRangeException) { return false; }
			catch (ArgumentOutOfRangeException) { return false; }
			return true;
		}
		private void DrawFrame(PaletteImage p, Point position, DrawSettings drawSettings, int frame) {

			graphicsData.paletteImages[frame].DrawWithOffset(p, position, drawSettings.Darkness, false,
				(Header.Flags.HasFlag(LargeSceneryFlags.Remap1) || Header.Flags.HasFlag(LargeSceneryFlags.Remap2)) ? drawSettings.Remap1 : RemapColors.None,
				(Header.Flags.HasFlag(LargeSceneryFlags.Remap2)) ? drawSettings.Remap2 : RemapColors.None,
				RemapColors.None
			);
		}
		/**<summary>Draws the object data in the dialog.</summary>*/
		public override bool DrawDialog(PaletteImage p, Point position, Size dialogSize, DrawSettings drawSettings) {
			try {
				graphicsData.paletteImages[drawSettings.Rotation].DrawWithOffset(p, Point.Add(position, new Size(dialogSize.Width / 2, (dialogSize.Height - 78) / 2)),
					drawSettings.Darkness, false,
					(Header.Flags.HasFlag(LargeSceneryFlags.Remap1) || Header.Flags.HasFlag(LargeSceneryFlags.Remap2)) ? drawSettings.Remap1 : RemapColors.None,
					(Header.Flags.HasFlag(LargeSceneryFlags.Remap2)) ? drawSettings.Remap2 : RemapColors.None,
					RemapColors.None
				);
			}
			catch (IndexOutOfRangeException) { return false; }
			catch (ArgumentOutOfRangeException) { return false; }
			return true;
		}
	
		#endregion
	}
	/**<summary>The header used for large scenery objects.</summary>*/
	public class LargeSceneryHeader : ObjectTypeHeader {
		//=========== MEMBERS ============
		#region Members

		/**<summary>Always zero in files.</summary>*/
		public ushort Reserved0;
		/**<summary>Always zero in files.</summary>*/
		public uint Reserved1;
		/**<summary>The cursor to use when placing the object.</summary>*/
		public byte Cursor;
		/**<summary>The flags used by the object.</summary>*/
		public LargeSceneryFlags Flags;
		/**<summary>The cost to build the object.</summary>*/
		public ushort BuildCost;
		/**<summary>The cost to remove the object.</summary>*/
		public short RemoveCost;
		/**<summary>Always zero in files.</summary>*/
		public uint Reserved2;
		/**<summary>Always zero in files.</summary>*/
		public byte Reserved3;
		/**<summary>0xFF if not scrolling, otherwise, indicates scrolling direction, location.</summary>*/
		public byte Scrolling;
		/**<summary>Always zero in files.</summary>*/
		public uint Reserved4;
		/**<summary>Always zero in files.</summary>*/
		public uint Reserved5;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the default object header.</summary>*/
		public LargeSceneryHeader() {
			Reserved0	= 0;
			Reserved1	= 0;
			Cursor		= 0;
			Flags		= LargeSceneryFlags.None;
			BuildCost	= 0;
			RemoveCost	= 0;
			Reserved2	= 0;
			Reserved3	= 0;
			Scrolling	= 0;
			Reserved4	= 0;
			Reserved5	= 0;
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>Gets the size of the object type header.</summary>*/
		internal override uint HeaderSize {
			get { return LargeScenery.HeaderSize; }
		}
		/**<summary>Gets the basic subtype of the object.</summary>*/
		internal override ObjectSubtypes ObjectSubtype {
			get {
				if (Flags.HasFlag(LargeSceneryFlags.Text3D))
					return ObjectSubtypes.Text3D;
				if (Flags.HasFlag(LargeSceneryFlags.TextScrolling))
					return ObjectSubtypes.TextScrolling;
				if (Flags.HasFlag(LargeSceneryFlags.Photogenic))
					return ObjectSubtypes.Photogenic;
				return ObjectSubtypes.Basic;
			}
		}

		#endregion
		//=========== READING ============
		#region Reading

		/**<summary>Reads the object header.</summary>*/
		internal override void Read(BinaryReader reader) {
			Reserved0	= reader.ReadUInt16();
			Reserved1	= reader.ReadUInt32();
			Cursor		= reader.ReadByte();
			Flags		= (LargeSceneryFlags)reader.ReadByte();
			BuildCost	= reader.ReadUInt16();
			RemoveCost	= reader.ReadInt16();
			Reserved2	= reader.ReadUInt32();
			Reserved3	= reader.ReadByte();
			Scrolling	= reader.ReadByte();
			Reserved4	= reader.ReadUInt32();
			Reserved5	= reader.ReadUInt32();
		}
		/**<summary>Writes the object header.</summary>*/
		internal override void Write(BinaryWriter writer) {
			writer.Write(Reserved0);
			writer.Write(Reserved1);
			writer.Write(Cursor);
			writer.Write((byte)Flags);
			writer.Write(BuildCost);
			writer.Write(RemoveCost);
			writer.Write(Reserved2);
			writer.Write(Reserved3);
			writer.Write(Scrolling);
			writer.Write(Reserved4);
			writer.Write(Reserved5);
		}

		#endregion
	}
	/**<summary>The header used for large scenery tiles.</summary>*/
	public struct LargeSceneryTileHeader {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The row index of the tile.</summary>*/
		public short Row;
		/**<summary>The column index of the tile.</summary>*/
		public short Column;
		/**<summary>The base height of the tile.</summary>*/
		public short BaseHeight;
		/**<summary>The clearance  of the tile.</summary>*/
		public byte Clearance;
		/**<summary>Bit 5 is set to indicate no supports.</summary>*/
		public byte Unknown1;
		/**<summary>The flags used by the tile.</summary>*/
		public LargeSceneryTileFlags Flags;

		/**<summary>The depth used for drawing tiles.</summary>*/
		public float Depth;
		/**<summary>The index used for drawing tiles.</summary>*/
		public int Index;

		#endregion
		//=========== READING ============
		#region Reading

		/**<summary>Reads the tile header.</summary>*/
		public void Read(BinaryReader reader) {
			Row = reader.ReadInt16();
			Column = reader.ReadInt16();
			BaseHeight = reader.ReadInt16();
			Clearance = reader.ReadByte();
			Unknown1 = reader.ReadByte();
			Flags = (LargeSceneryTileFlags)reader.ReadByte();
		}
		/**<summary>Writes the tile header.</summary>*/
		public void Write(BinaryWriter writer) {
			writer.Write(Row);
			writer.Write(Column);
			writer.Write(BaseHeight);
			writer.Write(Clearance);
			writer.Write(Unknown1);
			writer.Write((byte)Flags);
		}

		#endregion
	}
	/**<summary>All flags usable with large scenery objects.</summary>*/
	[Flags]
	public enum LargeSceneryFlags : byte {
		/**<summary>No flags are set.</summary>*/
		None = 0,
		/**<summary>Uses the first remappable color.</summary>*/
		Remap1 = 1 << 0,
		/**<summary>Uses the second remappable color.</summary>*/
		Remap2 = 1 << 1,
		/**<summary>Uses 3D text as a sign.</summary>*/
		Text3D = 1 << 2,
		/**<summary>Uses standard scrolling text technique.</summary>*/
		TextScrolling = 1 << 3,
		/**<summary>Guests appreciate the scenery object (and photograph it).</summary>*/
		Photogenic = 1 << 4,
	}
	/**<summary>All flags usable with large scenery tiles.</summary>*/
	[Flags]
	public enum LargeSceneryTileFlags : byte {
		/**<summary>No flags are set.</summary>*/
		None = 0,
		/**<summary>The north-west wall is placable.</summary>*/
		WallNW = 1 << 0,
		/**<summary>The north-east wall is placable.</summary>*/
		WallNE = 1 << 1,
		/**<summary>The south-east wall is placable.</summary>*/
		WallSE = 1 << 2,
		/**<summary>The south-west wall is placable.</summary>*/
		WallSW = 1 << 3,
		/**<summary>The east corner of the tile is occupied.</summary>*/
		CornerE = 1 << 4,
		/**<summary>The south corner of the tile is occupied.</summary>*/
		CornerS = 1 << 5,
		/**<summary>The west corner of the tile is occupied.</summary>*/
		CornerW = 1 << 6,
		/**<summary>The north corner of the tile is occupied.</summary>*/
		CornerN = 1 << 7,
	}
}
