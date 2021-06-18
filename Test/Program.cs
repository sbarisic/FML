using FishMarkupLanguage;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Test {
	class TestClass {
		public float[] items;
		public string[] items2;

		public TestClass2 TestItem;
	}

	class TestClass2 {
		public int Something;
	}

	class Program {
		static void Main(string[] args) {
			HTMLTest();
			CustomTest();
		}

		static void CustomTest() {
			FMLDocument Doc = FML.Parse("test_custom.fml");
			Doc.FlattenTemplates();

			TestClass TC = Doc.Deserialize<TestClass>();

			Console.WriteLine("Done!");
			Console.ReadLine();
		}

		static void HTMLTest() {
			FMLDocument Doc = FML.Parse("test_html.fml");
			Doc.FlattenTemplates();

			File.WriteAllText("out.fml", Doc.BuildString());

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
