﻿using RCT2ObjectData.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCT2ObjectData.Objects.Types {
	/**<summary>A wall object.</summary>*/
	public class Wall : ObjectData {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The size of the header for this object type.</summary>*/
		public const uint HeaderSize = 0x0E;

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The object header.</summary>*/
		public WallHeader Header;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the default object.</summary>*/
		public Wall() : base() {
			Header = new WallHeader();
		}
		/**<summary>Constructs the default object.</summary>*/
		internal Wall(ObjectDataHeader objectHeader, ChunkHeader chunkHeader)
			: base(objectHeader, chunkHeader) {
			Header = new WallHeader();
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
				if (Header.Flags.HasFlag(WallFlags.Door))
					return ObjectSubtypes.Door;
				if (Header.Flags.HasFlag(WallFlags.Glass))
					return ObjectSubtypes.Glass;
				if ((Header.Effects & 0x10) != 0x00)
					return ObjectSubtypes.Animation;
				if (Header.Scrolling != 0xFF)
					return ObjectSubtypes.TextScrolling;
				return ObjectSubtypes.Basic;
			}
		}
		/**<summary>True if the object can be placed on a slope.</summary>*/
		public override bool CanSlope {
			get { return !Header.Flags.HasFlag(WallFlags.Flat); }
		}
		/**<summary>Gets the number of color remaps.</summary>*/
		public override int ColorRemaps {
			get { return (Header.Flags.HasFlag(WallFlags.Remap3) ? 3 : (Header.Flags.HasFlag(WallFlags.Remap2) ? 2 : (Header.Flags.HasFlag(WallFlags.Remap1) ? 1 : 0))); }
		}
		/**<summary>Gets if the dialog view has color remaps.</summary>*/
		public override bool HasDialogColorRemaps {
			get { return true; }
		}
		/**<summary>Gets the number of frames in the animation.</summary>*/
		public override int AnimationFrames {
			get {
				if (Header.Flags.HasFlag(WallFlags.Door))
					return 5;
				if ((Header.Effects & 0x10) != 0x0)
					return 8;
				return 1;
			}
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

		#endregion
		//=========== DRAWING ============
		#region Drawing

		/**<summary>Constructs the default object.</summary>*/
		public override bool Draw(PaletteImage p, Point position, DrawSettings drawSettings) {
			try {
				bool flat = Header.Flags.HasFlag(WallFlags.Flat);
				bool twoSides = Header.Flags.HasFlag(WallFlags.TwoSides);
				bool door = Header.Flags.HasFlag(WallFlags.Door);
				bool glass = Header.Flags.HasFlag(WallFlags.Glass);
				bool animation = ((Header.Effects >> 4) & 0x1) == 0x1;

				int offset = (2 + (!flat ? 4 : 0)) * (twoSides ? 2 : 1) * (door ? 2 : 1);
				int slopeRotation = (drawSettings.Rotation % (twoSides ? 4 : 2)) * (door ? 2 : 1);
				if (drawSettings.Slope >= 0 && !flat) {
					if (drawSettings.Slope % 2 != drawSettings.Rotation % 2) {
						if (drawSettings.Slope >= 2) slopeRotation = (7 - drawSettings.Slope) * (door ? 2 : 1);
						else slopeRotation = (3 - drawSettings.Slope) * (door ? 2 : 1);
					}
				}

				DrawFrame(p, position, drawSettings, slopeRotation + drawSettings.Frame * offset, false);
				if (door)
					DrawFrame(p, position, drawSettings, slopeRotation + 1 + drawSettings.Frame * offset, false);
				if (glass)
					DrawFrame(p, position, drawSettings, offset + slopeRotation + drawSettings.Frame * offset, true);
			}
			catch (IndexOutOfRangeException) { return false; }
			catch (ArgumentOutOfRangeException) { return false; }
			return true;
		}
		/**<summary>Draws the object data in the dialog.</summary>*/
		public override bool DrawDialog(PaletteImage p, Point position, Size dialogSize, DrawSettings drawSettings) {
			try {
				bool flat = Header.Flags.HasFlag(WallFlags.Flat);
				bool twoSides = Header.Flags.HasFlag(WallFlags.TwoSides);
				bool door = Header.Flags.HasFlag(WallFlags.Door);
				bool glass = Header.Flags.HasFlag(WallFlags.Glass);

				int offset = (2 + (!flat ? 4 : 0)) * (twoSides ? 2 : 1) * (door ? 2 : 1);

				DrawDialogFrame(p, Point.Add(position, new Size(dialogSize.Width / 2, dialogSize.Height / 2)), drawSettings, 0, false);
				if (door)
					DrawDialogFrame(p, Point.Add(position, new Size(dialogSize.Width / 2, dialogSize.Height / 2)), drawSettings, 1, false);
				if (glass)
					DrawDialogFrame(p, Point.Add(position, new Size(dialogSize.Width / 2, dialogSize.Height / 2)), drawSettings, offset, true);
			}
			catch (IndexOutOfRangeException) { return false; }
			catch (ArgumentOutOfRangeException) { return false; }
			return true;
		}
		private void DrawFrame(PaletteImage p, Point position, DrawSettings drawSettings, int frame, bool glass) {
			Size offset = Size.Empty;
			if (drawSettings.Slope >= 2) {
				drawSettings.Rotation = (drawSettings.Rotation + 2) % 4;
			}
			if (drawSettings.Slope != -1 && !Header.Flags.HasFlag(WallFlags.Flat)) {
				if (drawSettings.Slope == drawSettings.Rotation) {
					if (drawSettings.Slope < 2) offset.Height = -16;
				}
				else if (drawSettings.Slope % 2 != drawSettings.Rotation % 2) {
					switch (drawSettings.Slope) {
					case 0: if (drawSettings.Rotation == 3) { offset.Width = -32; offset.Height = 16; } break;
					case 1: if (drawSettings.Rotation == 2) { offset.Width =  32; offset.Height = 16; } break;
					case 2: if (drawSettings.Rotation == 1) { offset.Width = -32; offset.Height = 16; } break;
					case 3: if (drawSettings.Rotation == 0) { offset.Width =  32; offset.Height = 16; } break;
					}
				}
				else {
					if (drawSettings.Slope < 2) offset.Height = 16;
					offset.Width = (drawSettings.Slope % 2 == 0 ? 32 : -32);
				}
			}
			else if (drawSettings.Rotation == 2) { offset.Width = 32; offset.Height = 16; }
			else if (drawSettings.Rotation == 3) { offset.Width = -32; offset.Height = 16; }

			graphicsData.paletteImages[frame].DrawWithOffset(p, Point.Add(position, offset), drawSettings.Darkness, glass,
				(Header.Flags.HasFlag(WallFlags.Remap1) || Header.Flags.HasFlag(WallFlags.Remap2) || Header.Flags.HasFlag(WallFlags.Remap3)) ? drawSettings.Remap1 : (glass ? drawSettings.Remap1 : RemapColors.None),
				(Header.Flags.HasFlag(WallFlags.Remap2) || Header.Flags.HasFlag(WallFlags.Remap3)) ? (glass ? drawSettings.Remap1 : drawSettings.Remap2) : (glass ? drawSettings.Remap1 : RemapColors.None),
				Header.Flags.HasFlag(WallFlags.Remap3) ? (glass ? drawSettings.Remap1 : drawSettings.Remap3) : (glass ? drawSettings.Remap1 : RemapColors.None)
			);
		}
		private void DrawDialogFrame(PaletteImage p, Point position, DrawSettings drawSettings, int frame, bool glass) {
			Size offset = new Size(16, 16);

			graphicsData.paletteImages[frame].DrawWithOffset(p, Point.Add(position, offset), drawSettings.Darkness, glass,
				(Header.Flags.HasFlag(WallFlags.Remap1) || Header.Flags.HasFlag(WallFlags.Remap2) || Header.Flags.HasFlag(WallFlags.Remap3)) ? drawSettings.Remap1 : (glass ? drawSettings.Remap1 : RemapColors.None),
				(Header.Flags.HasFlag(WallFlags.Remap2) || Header.Flags.HasFlag(WallFlags.Remap3)) ? (glass ? drawSettings.Remap1 : drawSettings.Remap2) : (glass ? drawSettings.Remap1 : RemapColors.None),
				Header.Flags.HasFlag(WallFlags.Remap3) ? (glass ? drawSettings.Remap1 : drawSettings.Remap3) : (glass ? drawSettings.Remap1 : RemapColors.None)
			);
		}

		#endregion
	}
	/**<summary>The header used for wall objects.</summary>*/
	public class WallHeader : ObjectTypeHeader {
		//=========== MEMBERS ============
		#region Members

		/**<summary>Always zero in files.</summary>*/
		public ushort Reserved0;
		/**<summary>Always zero in files.</summary>*/
		public uint Reserved1;
		/**<summary>The cursor to use when placing the object.</summary>*/
		public byte Cursor;
		/**<summary>The flags used by the object.</summary>*/
		public WallFlags Flags;
		/**<summary>The height of the object.</summary>*/
		public byte Clearance;
		/**<summary>first nibble = visibility (0 = opaque), upper nibble: 1 = animated (8 frames)</summary>*/
		public byte Effects;
		/**<summary>The cost to build the object x 10.</summary>*/
		public ushort BuildCost;
		/**<summary>Always zero in files.</summary>*/
		public byte Reserved2;
		/**<summary>0xFF if not scrolling.</summary>*/
		public byte Scrolling;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the default object header.</summary>*/
		public WallHeader() {
			Reserved0	= 0;
			Reserved1	= 0;
			Cursor		= 0;
			Flags		= WallFlags.None;
			Clearance	= 32;
			Effects		= 0;
			BuildCost	= 1;
			Reserved2	= 0;
			Scrolling	= 0xFF;
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>Gets the size of the object type header.</summary>*/
		internal override uint HeaderSize {
			get { return Wall.HeaderSize; }
		}
		/**<summary>Gets the basic subtype of the object.</summary>*/
		internal override ObjectSubtypes ObjectSubtype {
			get {
				if (Flags.HasFlag(WallFlags.Door))
					return ObjectSubtypes.Door;
				if (Flags.HasFlag(WallFlags.Glass))
					return ObjectSubtypes.Glass;
				if ((Effects & 0x10) != 0x00)
					return ObjectSubtypes.Animation;
				if (Scrolling != 0xFF)
					return ObjectSubtypes.TextScrolling;
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
			Flags		= (WallFlags)reader.ReadByte();
			Clearance	= reader.ReadByte();
			Effects		= reader.ReadByte();
			BuildCost	= reader.ReadUInt16();
			Reserved2	= reader.ReadByte();
			Scrolling	= reader.ReadByte();
		}
		/**<summary>Writes the object header.</summary>*/
		internal override void Write(BinaryWriter writer) {
			writer.Write(Reserved0);
			writer.Write(Reserved1);
			writer.Write(Cursor);
			writer.Write((byte)Flags);
			writer.Write(Clearance);
			writer.Write(Effects);
			writer.Write(BuildCost);
			writer.Write(Reserved2);
			writer.Write(Scrolling);
		}

		#endregion
	}
	/**<summary>All flags usable with wall objects.</summary>*/
	[Flags]
	public enum WallFlags : byte {
		/**<summary>No flags are set.</summary>*/
		None = 0,
		/**<summary>Uses the first remappable color</summary>*/
		Remap1 = 1 << 0,
		/**<summary>A "glass" object: the first image is the "frame" and the second image is the "glass"</summary>*/
		Glass = 1 << 1,
		/**<summary>Must be on a flat surface. Also, walls can't occupy the same tile</summary>*/
		Flat = 1 << 2,
		/**<summary>Has a front and a back</summary>*/
		TwoSides = 1 << 3,
		/**<summary>Special processing for doorways (36 images).</summary>*/
		Door = 1 << 4,
		/**<summary>Uses the second remappable color</summary>*/
		Remap2 = 1 << 6,
		/**<summary>Uses the third remappable color</summary>*/
		Remap3 = 1 << 7
	}
}
