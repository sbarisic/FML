using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TokenTuple = System.Tuple<string, FishMarkupLanguage.TokenType>;

namespace FishMarkupLanguage {
	public class FMLTag {
		public string TagName;

		public FMLTag Parent;
		public List<FMLTag> Children;

		public FMLTag(string Name) {
			TagName = Name;
			Children = new List<FMLTag>();
			Parent = null;
		}

		public string BuildString() {
			StringBuilder SB = new StringBuilder().Append(TagName).Append(" ").AppendLine("{");

			foreach (var Child in Children)
				SB.AppendLine(Child.BuildString());


			return SB.AppendLine("}").ToString();
		}

		public FMLTag CreateChild(string Name) {
			FMLTag C = new FMLTag(Name);
			C.Parent = this;
			Children.Add(C);
			return C;
		}

		public override string ToString() {
			return string.Format("{0} {{ {1} }}", TagName, Children?.Count ?? 0);
		}
	}

	public class FMLDocument {
		public FMLTag Root;

		public FMLDocument() {
			Root = new FMLTag("root");
		}
	}

	public static class FML {
		static TokenTuple[] SymbolTypes = new[] {
			new TokenTuple("$", TokenType.PARAM_REF),
			new TokenTuple("{", TokenType.BEGIN_BLOCK),
			new TokenTuple("}", TokenType.END_BLOCK),
			new TokenTuple("=", TokenType.ASSIGN),
			new TokenTuple(";", TokenType.DELIMITER)
		};

		static string[] Keywords = new[] { "template", "none" };

		static bool IsIDChar(char C) {
			return char.IsLetterOrDigit(C) || C == '_' || C == '-';
		}

		static bool IsSymbol(char C) {
			if (IsIDChar(C))
				return false;

			return C == '=' || C == '{' || C == '}' || C == ';' || char.IsSymbol(C) || char.IsPunctuation(C);
		}

		static bool IsWhiteSpace(char C) {
			return char.IsWhiteSpace(C);
		}

		public static FMLDocument Parse(string FileName) {
			string Source = File.ReadAllText(FileName).Replace("\r", "");
			StringBuilder CmntCleaner = new StringBuilder();

			bool InSingleLine = false;
			bool InMultiLine = false;
			for (int i = 0; i < Source.Length; i++) {
				if (InSingleLine) {
					while (Source[i] != '\n')
						i++;

					i--;
					InSingleLine = false;
					continue;
				} else if (InMultiLine) {
					while (!(Source[i] == '*' && Source[i + 1] == '/')) {
						if (Source[i] == '\n')
							CmntCleaner.Append('\n');
						i++;
					}

					i++;
					InMultiLine = false;
					continue;
				}

				if (Source[i] == '/' && Source[i + 1] == '/') {
					InSingleLine = true;
					continue;
				} else if (Source[i] == '/' && Source[i + 1] == '*') {
					InMultiLine = true;
					continue;
				}

				CmntCleaner.Append(Source[i]);
			}

			Source = CmntCleaner.ToString();


			bool InQuote = false;
			StringBuilder CurTok = new StringBuilder();
			List<Token> Tokens = new List<Token>();

			for (int i = 0; i < Source.Length; i++) {
				char C = Source[i];
				char PC = i > 0 ? Source[i - 1] : (char)0;
				char PPC = i > 1 ? Source[i - 2] : (char)0;

				if (C == '\"') {
					if (!InQuote) {
						InQuote = true;
						if (CurTok.Length > 0)
							EmitToken(CurTok.ToString(), Tokens);
						CurTok.Clear();
					} else if (PC != '\\' || (PC == '\\' && PPC == '\\')) {
						InQuote = false;
						EmitToken("\"" + CurTok.ToString() + "\"", Tokens);
						CurTok.Clear();
					} else
						CurTok.Append(C);
					continue;
				}

				if (!InQuote) {
					if (IsWhiteSpace(C)) {
						if (CurTok.Length > 0) {
							EmitToken(CurTok.ToString(), Tokens);
							CurTok.Clear();
						}
					} else if (IsSymbol(C)) {
						if (CurTok.Length > 0) {
							EmitToken(CurTok.ToString(), Tokens);
							CurTok.Clear();
						}

						EmitToken(C.ToString(), Tokens);
					} else {
						CurTok.Append(C);
					}
				} else if (InQuote)
					CurTok.Append(C);
			}

			if (CurTok.Length > 0)
				EmitToken(CurTok.ToString(), Tokens);

			FMLDocument Doc = new FMLDocument();
			FMLTag CurTag = Doc.Root;
			
			return Doc;
		}

		static void EmitToken(string Tok, List<Token> Tokens) {
			Token T = new Token() { Src = Tok };

			if (Tok.Length == 1 && IsSymbol(Tok[0])) {
				T.Tok = SymbolTypes.Where(TT => TT.Item1 == Tok).FirstOrDefault()?.Item2 ?? TokenType.NONE;

				// Special case to replace semicolon with an empty block
				if (T.Tok == TokenType.DELIMITER) {
					Tokens.Add(new Token() { Src = "{", Tok = TokenType.BEGIN_BLOCK });
					T.Src = "}";
					T.Tok = TokenType.END_BLOCK;
				}
			} else if (Tok.StartsWith("\"") && Tok.EndsWith("\"")) {
				T.Tok = TokenType.QUOTE;
			} else {
				bool ValidID = true;

				for (int i = 0; i < Tok.Length; i++) {
					if (!IsIDChar(Tok[i])) {
						ValidID = false;
						break;
					}
				}

				if (ValidID) {
					if (Keywords.Contains(Tok))
						T.Tok = TokenType.KEYWORD;
					else
						T.Tok = TokenType.ID;
				}
			}

			if (T.Tok == TokenType.NONE)
				throw new NotImplementedException();

			Tokens.Add(T);
		}

	}

	struct Token {
		public string Src;
		public TokenType Tok;

		public override string ToString() {
			string TokName = Tok.ToString();
			int TokLen = 16;

			if (TokName.Length < TokLen)
				TokName += new string(' ', TokLen - TokName.Length);

			return string.Format("{0} {1}", TokName, Src);
		}
	}

	enum TokenType {
		NONE,
		ID,
		PARAM_REF,
		BEGIN_BLOCK,
		END_BLOCK,
		ASSIGN,
		DELIMITER,
		KEYWORD,
		QUOTE
	}
}
