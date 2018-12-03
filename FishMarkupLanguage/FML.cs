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
		static int Line;
		static int Col;

		static TokenTuple[] SymbolTypes = new[] {
			new TokenTuple("$", TokenType.DOLLAR),
			new TokenTuple("#", TokenType.HASH),
			new TokenTuple("{", TokenType.BRACKET_OPEN),
			new TokenTuple("}", TokenType.BRACKET_CLOSE),
			new TokenTuple("=", TokenType.EQUAL),
			new TokenTuple(";", TokenType.SEMICOLON),
			new TokenTuple(".", TokenType.DOT),
		};

		static string[] Keywords = new[] { "template", "none" };

		static bool IsSymbol(char C) {
			if (SymbolTypes.Select(T => T.Item1).Contains(C.ToString()))
				return true;

			return false;
		}

		static bool IsNewLine(char C) {
			if (C == '\n')
				return true;

			return false;
		}

		static bool IsWhiteSpace(char C) {
			return IsNewLine(C) || char.IsWhiteSpace(C);
		}

		static bool IsIDChar(char C) {
			if (IsWhiteSpace(C) || IsSymbol(C))
				return false;

			//return char.IsLetterOrDigit(C) || C == '_' || C == '-';
			return true;
		}

		static void Fail(string Msg) {
			throw new Exception(string.Format("{0}:{1}; {2}", Line + 1, Col, Msg));
		}

		static void Fail(string Fmt, params object[] Args) {
			Fail(string.Format(Fmt, Args));
		}

		public static FMLDocument Parse(string FileName) {
			string Source = File.ReadAllText(FileName).Replace("\r", "");

			bool InQuote = false;
			bool InSLComment = false;
			bool InMLComment = false;

			bool InDocument = false;
			int DocEqLen = -1;

			StringBuilder CurTok = new StringBuilder();
			List<Token> Tokens = new List<Token>();
			Action EmitIfNotEmpty = () => {
				if (CurTok.Length > 0) {
					EmitToken(CurTok.ToString(), Tokens);
					CurTok.Clear();
				}
			};

			Line = Col = 0;

			for (int i = 0; i < Source.Length; i++) {
				char C = Source[i];
				char PC = i > 0 ? Source[i - 1] : (char)0;
				char PPC = i > 1 ? Source[i - 2] : (char)0;
				char NC = (i < Source.Length - 1) ? Source[i + 1] : (char)0;

				Col++;
				if (IsNewLine(C)) {
					Col = 0;
					Line++;
				}

				if (!InMLComment && !InDocument) {
					if (C == '/' && NC == '/') {
						EmitIfNotEmpty();
						InSLComment = true;
					}

					if (InSLComment) {
						if (IsNewLine(C)) {
							InSLComment = false;
							EmitIfNotEmpty();
						}

						continue;
					}
				}

				if (!InSLComment && !InDocument) {
					if (C == '/' && NC == '*') {
						EmitIfNotEmpty();
						InMLComment = true;
					}

					if (InMLComment) {
						if (C == '*' && NC == '/') {
							i++;
							InMLComment = false;
							EmitIfNotEmpty();
						}

						continue;
					}
				}

				if (InDocument) {
					if (C == ']') {
						int BeginI = i;
						bool ValidEnd = true;
						int PrevDocEqLen = DocEqLen;

						i++;

						while (DocEqLen > 0) {
							string END = Source.Substring(i);
							char CCCC = Source[i + DocEqLen - 1];

							if (Source[i + DocEqLen - 1] != '=') {
								ValidEnd = false;
								break;
							}

							DocEqLen--;
						}

						i += PrevDocEqLen;

						if (ValidEnd && Source[i] != ']') 
							ValidEnd = false;

						if (ValidEnd) {
							Tokens.Add(new Token(CurTok.ToString(), TokenType.DOCUMENT));
							CurTok.Clear();
							InDocument = false;
							continue;
						} else {
							i = BeginI;
							DocEqLen = PrevDocEqLen;
						}
					}

					CurTok.Append(C);
					continue;
				}

				if (C == '[') {
					EmitIfNotEmpty();
					InDocument = true;
					DocEqLen = 0;
					i++;

					while (Source[i + DocEqLen] == '=')
						DocEqLen++;

					if (Source[i + DocEqLen] != '[')
						Fail("Expected [, got {0}", Source[i + DocEqLen]);

					i += DocEqLen;
					continue;
				}

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

			foreach (var T in Tokens)
				Console.WriteLine(T);

			FMLDocument Doc = new FMLDocument();
			FMLTag CurTag = Doc.Root;
			return Doc;
		}

		static void EmitToken(string Tok, List<Token> Tokens) {
			Token T = new Token() { Src = Tok };

			if (Tok.Length == 1 && IsSymbol(Tok[0])) {
				T.Tok = SymbolTypes.Where(TT => TT.Item1 == Tok).FirstOrDefault()?.Item2 ?? TokenType.NONE;

				/*// Special case to replace semicolon with an empty block
				if (T.Tok == TokenType.SEMICOLON) {
					Tokens.Add(new Token() { Src = "{", Tok = TokenType.BRACKET_OPEN });
					T.Src = "}";
					T.Tok = TokenType.BRACKET_CLOSE;
				}*/
			} else if (Tok.StartsWith("\"") && Tok.EndsWith("\"")) {
				T.Tok = TokenType.STRING;
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
						T.Tok = TokenType.IDENTIFIER; // TODO: Keyword token?
					else
						T.Tok = TokenType.IDENTIFIER;
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

		public Token(string Src, TokenType Tok) {
			this.Src = Src;
			this.Tok = Tok;
		}

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

		IDENTIFIER,
		NUMBER,
		STRING,
		EQUAL,
		BRACKET_OPEN,
		BRACKET_CLOSE,
		DOCUMENT,
		SEMICOLON,
		DOT,
		DOLLAR,
		HASH,
		EOF
	}
}
