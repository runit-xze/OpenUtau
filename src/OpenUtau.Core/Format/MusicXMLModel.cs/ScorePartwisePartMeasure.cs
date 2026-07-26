using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace OpenUtau.Core.Format.MusicXMLSchema {
	public interface IMeasureElement { }

	public partial class ScorePartwisePartMeasure : IMeasureAttributes, IXmlSerializable {

		public ScorePartwisePartMeasure() { }

		[XmlIgnore] public List<IMeasureElement> Content { get; set; } = new List<IMeasureElement>();

		[XmlIgnore] public List<Note> Notes => Content.OfType<Note>().ToList();
		[XmlIgnore] public List<Backup> Backups => Content.OfType<Backup>().ToList();
		[XmlIgnore] public List<Forward> Forwards => Content.OfType<Forward>().ToList();
		[XmlIgnore] public List<Direction> Directions => Content.OfType<Direction>().ToList();
		[XmlIgnore] public List<Attributes> Attributes => Content.OfType<Attributes>().ToList();
		[XmlIgnore] public List<Harmony> Harmonies => Content.OfType<Harmony>().ToList();
		[XmlIgnore] public List<FiguredBass> FiguredBasses => Content.OfType<FiguredBass>().ToList();
		[XmlIgnore] public List<Print> Prints => Content.OfType<Print>().ToList();
		[XmlIgnore] public List<Sound> Sounds => Content.OfType<Sound>().ToList();
		[XmlIgnore] public List<Listening> Listenings => Content.OfType<Listening>().ToList();
		[XmlIgnore] public List<Barline> Barlines => Content.OfType<Barline>().ToList();
		[XmlIgnore] public List<Grouping> Groupings => Content.OfType<Grouping>().ToList();
		[XmlIgnore] public List<Link> Links => Content.OfType<Link>().ToList();
		[XmlIgnore] public List<Bookmark> Bookmarks => Content.OfType<Bookmark>().ToList();


		[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
		[XmlAttribute("number")]
		public string Number { get; set; } = "0";

		[System.ComponentModel.DataAnnotations.MinLength(1)]
		[XmlAttribute("text")]
		public string Text { get; set; }

		[XmlAttribute("implicit")]
		public YesNo Implicit { get; set; } = YesNo.No;

		[XmlIgnore()]
		public bool ImplicitSpecified { get; set; }

		[XmlAttribute("non-controlling")]
		public YesNo NonControlling { get; set; }

		[XmlIgnore()]
		public bool NonControllingSpecified { get; set; }

		[XmlAttribute("width")]
		public decimal Width { get; set; }

		[XmlIgnore()]
		public bool WidthSpecified { get; set; }

		[XmlAttribute("id")]
		public string Id { get; set; }

		public System.Xml.Schema.XmlSchema GetSchema() => null;

		public void WriteXml(XmlWriter writer) {
			throw new System.NotImplementedException();
		}

		public void ReadXml(XmlReader reader) {
			// 1. Read and set Measure's own attributes (e.g. "number")
			if (reader.MoveToAttribute("number")) {
				this.Number = reader.Value;
				reader.MoveToElement();
			}
			if (reader.MoveToAttribute("id")) {
				this.Id = reader.Value;
				reader.MoveToElement();
			}
			if (reader.MoveToAttribute("implicit")) {
				if (YesNo.TryParse(reader.Value, out YesNo implicitValue)) {
					this.Implicit = implicitValue;
					this.ImplicitSpecified = true;
				}
				reader.MoveToElement();
			}
			if (reader.MoveToAttribute("non-controlling")) {
				if (YesNo.TryParse(reader.Value, out YesNo nonControllingValue)) {
					this.NonControlling = nonControllingValue;
					this.NonControllingSpecified = true;
				}
				reader.MoveToElement();
			}
			if (reader.MoveToAttribute("text")) {
				this.Text = reader.Value;
				reader.MoveToElement();
			}
			if (reader.MoveToAttribute("width")) {
				if (decimal.TryParse(reader.Value, out decimal widthValue)) {
					this.Width = widthValue;
					this.WidthSpecified = true;
				}
				reader.MoveToElement();
			}

			// 2. Enter the Measure element (read start tag)
			reader.ReadStartElement();

			// 3. Loop to read all child elements inside Measure
			while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None) {
				if (reader.NodeType == XmlNodeType.Element) {
					IMeasureElement element = null;
					string elementName = reader.LocalName;

					// All possible elements in measure: 
					// https://www.w3.org/2021/06/musicxml40/musicxml-reference/elements/measure-partwise/
					switch (elementName) {
						case "note":
							element = DeserializeElement<Note>(reader);
							break;
						case "backup":
							element = DeserializeElement<Backup>(reader);
							break;
						case "forward":
							element = DeserializeElement<Forward>(reader);
							break;
						case "direction":
							element = DeserializeElement<Direction>(reader);
							break;
						case "attributes":
							element = DeserializeElement<Attributes>(reader);
							break;
						case "harmony":
							element = DeserializeElement<Harmony>(reader);
							break;
						case "figured-bass":
							element = DeserializeElement<FiguredBass>(reader);
							break;
						case "print":
							element = DeserializeElement<Print>(reader);
							break;
						case "sound":
							element = DeserializeElement<Sound>(reader);
							break;
						case "listening":
							element = DeserializeElement<Listening>(reader);
							break;
						case "barline":
							element = DeserializeElement<Barline>(reader);
							break;
						case "grouping":
							element = DeserializeElement<Grouping>(reader);
							break;
						case "link":
							element = DeserializeElement<Link>(reader);
							break;
						case "bookmark":
							element = DeserializeElement<Bookmark>(reader);
							break;
						default:
							// Skip unknown elements directly
							reader.Skip();
							break;
					}

					if (element != null) {
						// Add elements in the order they appear in XML
						this.Content.Add(element);
					}
				} else {
					// Skip non-element nodes (such as whitespace, comments)
					reader.Read();
				}
			}

			// 4. Read the end tag of Measure element
			reader.ReadEndElement();
		}

		private T DeserializeElement<T>(XmlReader reader) where T : IMeasureElement {
			// 使用一个临时的 XmlSerializer 来处理 T 类型的元素
			var serializer = new XmlSerializer(typeof(T));

			// Deserialize 方法会自动消耗掉 T 元素的起始标签和结束标签
			// 所以主循环的 reader 会自动定位到下一个元素。
			return (T)serializer.Deserialize(reader);
		}
	}
}
