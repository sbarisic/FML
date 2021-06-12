using FishMarkupLanguage;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Test {
	class Program {
		static void Main(string[] args) {
			FMLDocument Doc = FML.Parse("test_working.fml");
			Doc.FlattenTemplates();

			StringBuilder SB = new StringBuilder();
			Doc.BuildString(SB);
			File.WriteAllText("out.fml", SB.ToString());

			XmlDocument XmlDoc = Doc.ToXML();
			File.WriteAllText("out.html", XmlToString(XmlDoc));
		}

		static string XmlToString(XmlDocument Doc) {
			StringWriter SW = new StringWriter();

			XmlTextWriter TX = new XmlTextWriter(SW);
			TX.Formatting = Formatting.Indented;

			Doc.WriteTo(TX);
			return SW.ToString();
		}
	}
}
