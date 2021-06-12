using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishMarkupLanguage {
	public class FMLTagSet {
		List<string> Tags;

		public bool AnyTagValid {
			get; set;
		}

		public FMLTagSet() {
			Tags = new List<string>();
			AnyTagValid = false;
		}

		public void AddTag(string Name) {
			Tags.Add(Name);
		}

		public void AddTags(params string[] Names) {
			foreach (var T in Names)
				AddTag(T);
		}

		public bool IsValid(string Name) {
			if (AnyTagValid)
				return true;

			if (Tags.Contains(Name))
				return true;

			return false;
		}

		public string[] GetAllTags() {
			return Tags.ToArray();
		}
	}

}
