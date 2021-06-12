using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

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
	}

	public class FMLHereDoc {
		public string Content;

		public FMLHereDoc(string Content) {
			this.Content = Content;
		}
	}
}
