using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;

namespace FishMarkupLanguage {

	public class FMLDocument {
		public FMLTagSet TagSet;
		public List<FMLTag> Tags;
		public List<FMLTemplateTag> Templates;

		public FMLDocument() {
			TagSet = new FMLTagSet();
			Tags = new List<FMLTag>();
			Templates = new List<FMLTemplateTag>();
		}

		public void FlattenTemplates() {
			for (int i = 0; i < Tags.Count; i++) {
				if (FlattenTemplate(Tags[i], out FMLTag[] NewTags)) {
					Tags.RemoveAt(i);
					Tags.InsertRange(i, NewTags);
				}
			}
		}

		bool ContainsTemplate(string Name, out FMLTemplateTag TTag) {
			foreach (FMLTemplateTag T in Templates) {
				if (T.TemplateName == Name) {
					TTag = T;
					return true;
				}
			}

			TTag = null;
			return false;
		}

		bool FlattenTemplate(FMLTag TemplateInvoke, out FMLTag[] NewTags) {
			if (ContainsTemplate(TemplateInvoke.TagName, out FMLTemplateTag TTag)) {
				NewTags = TTag.ConstructTags(TemplateInvoke);
				return true;
			} else {
				for (int i = 0; i < TemplateInvoke.Children.Count; i++) {
					if (FlattenTemplate(TemplateInvoke.Children[i], out FMLTag[] NewChildTags)) {
						TemplateInvoke.Children.RemoveAt(i);
						TemplateInvoke.Children.InsertRange(i, NewChildTags);
					}
				}
			}

			NewTags = null;
			return false;
		}

		public XmlDocument ToXML() {
			XmlDocument XMLDoc = new XmlDocument();

			foreach (FMLTag T in Tags) {
				XmlElement E = T.ToXmlElement(XMLDoc);

				if (E == null)
					throw new Exception();

				XMLDoc.AppendChild(E);
			}

			return XMLDoc;
		}

		public void BuildString(StringBuilder SB) {
			foreach (FMLTag T in Tags) {
				T.BuildString(0, SB);
			}
		}

		public string BuildString() {
			StringBuilder SB = new StringBuilder();
			BuildString(SB);
			return SB.ToString();
		}

		public T Deserialize<T>() where T : class, new() {
			T Obj = (T)DeserializeInstance(typeof(T), Tags);
			return Obj;
		}

		object DeserializeInstance(Type T, List<FMLTag> TagSet) {
			object Obj = Activator.CreateInstance(T);
			FieldInfo[] Fields = Utils.GetFields(T);

			foreach (FMLTag Tg in TagSet) {
				FieldInfo FI = Fields.Where(F => F.Name == Tg.TagName).FirstOrDefault();
				object FieldValue = null;

				if (FI.FieldType.IsArray) {
					Type ElementType = FI.FieldType.GetElementType();
					Array ElementArray = Array.CreateInstance(ElementType, Tg.Children.Count);

					for (int i = 0; i < Tg.Children.Count; i++) {
						if (Tg.Children[i] is FMLValueTag ValTag) {
							ElementArray.SetValue(ValTag.Value, i);
						} else
							throw new Exception("Expected value tag");
					}

					FieldValue = ElementArray;
				} else if (Utils.TryCreateInstance(FI.FieldType, Tg.Children[0] as FMLValueTag, out FieldValue)) {
				} else {
					FieldValue = DeserializeInstance(FI.FieldType, Tg.Children);
				}

				FI.SetValue(Obj, FieldValue);
			}

			return Obj;
		}
	}

	public class FMLHereDoc {
		public int Level;
		public string Content;

		public FMLHereDoc(int Level, string Content) {
			this.Level = Level;
			this.Content = Content;
		}

		public string ToHereDocString() {
			return string.Format("[{0}[{1}]{0}]", new string('=', Level), Content);
		}

		public override string ToString() {
			return Content;
		}
	}
}
