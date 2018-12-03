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

		static bool CanNumStartWith(char C, char NC) {
			if (C == '.' && char.IsDigit(NC))
				return true;

			return char.IsDigit(C) || C == '+' || C == '-';
		}

		static bool IsNumberElement(char C) {
			return C == '+' || C == '-' || C == 'E' || C == 'e' || C == 'x' || C == 'o' || C == 'b' || C == 'f' || C == '.' || char.IsDigit(C);
		}

		static void Fail(string Msg) {
			throw new Exception(string.Format("{0}:{1}; {2}", Line, Col, Msg));
		}

		static void Fail(string Fmt, params object[] Args) {
			Fail(string.Format(Fmt, Args));
		}

		public static FMLDocument Parse(string FileName) {
			string Source = File.ReadAllText(FileName).Replace("\r", "") + "\n";

			bool InQuote = false;
			bool InSLComment = false;
			bool InMLComment = false;
			bool InNumber = false;
			bool InDocument = false;
			int DocEqLen = -1;

			StringBuilder CurTok = new StringBuilder();
			List<Token> Tokens = new List<Token>();
			Action<TokenType> EmitIfNotEmpty = (TT) => {
				if (CurTok.Length > 0) {
					EmitToken(CurTok.ToString(), Tokens, TT);
					CurTok.Clear();
				}
			};

			Line = 1;
			Col = 0;

			for (int i = 0; i < Source.Length; i++) {
				char C = Source[i];
				char PC = i > 0 ? Source[i - 1] : (char)0;
				char PPC = i > 1 ? Source[i - 2] : (char)0;
				char NC = (i < Source.Length - 1) ? Source[i + 1] : (char)0;

				if (InNumber) {
					if (IsNumberElement(C)) {
						CurTok.Append(C);
						continue;
					} else {
						InNumber = false;
						EmitIfNotEmpty(TokenType.NUMBER);
					}
				}

				Col++;
				if (IsNewLine(C)) {
					Col = 0;
					Line++;
				}

				if (!InMLComment && !InDocument) {
					if (C == '/' && NC == '/') {
						EmitIfNotEmpty(TokenType.NONE);
						InSLComment = true;
					}

					if (InSLComment) {
						if (IsNewLine(C)) {
							InSLComment = false;
							EmitIfNotEmpty(TokenType.NONE);
						}

						continue;
					}
				}

				if (!InSLComment && !InDocument) {
					if (C == '/' && NC == '*') {
						EmitIfNotEmpty(TokenType.NONE);
						InMLComment = true;
					}

					if (InMLComment) {
						if (C == '*' && NC == '/') {
							i++;
							InMLComment = false;
							EmitIfNotEmpty(TokenType.NONE);
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
							Tokens.Add(new Token(CurTok.ToString(), TokenType.DOCUMENT, Line, Col));
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
					EmitIfNotEmpty(TokenType.NONE);
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

				if (InQuote) {
					CurTok.Append(C);
					continue;
				}

				if (IsWhiteSpace(C))
					EmitIfNotEmpty(TokenType.NONE);
				else if (CurTok.Length == 0 && CanNumStartWith(C, NC)) {
					InNumber = true;
					CurTok.Append(C);
				} else if (IsSymbol(C)) {
					/*if (CurTok.Length == 0 && CanNumStartWith(C, NC)) {
						InNumber = true;
						CurTok.Append(C);
						continue;
					}*/

					EmitIfNotEmpty(TokenType.NONE);
					EmitToken(C.ToString(), Tokens);
				} else {
					// Numbers
					/*if (CurTok.Length == 0 && CanNumStartWith(C, NC))
						InNumber = true;*/

					CurTok.Append(C);
				}
			}

			if (CurTok.Length > 0)
				EmitToken(CurTok.ToString(), Tokens);

			foreach (var T in Tokens)
				Console.WriteLine(T);

			FMLDocument Doc = new FMLDocument();
			FMLTag CurTag = Doc.Root;
			return Doc;
		}

		static void EmitToken(string Tok, List<Token> Tokens, TokenType TokType = TokenType.NONE) {
			Token T = new Token(Tok, TokType, Line, Col);

			if (Tok.Length == 1 && IsSymbol(Tok[0])) {
				T.Tok = SymbolTypes.Where(TT => TT.Item1 == Tok).FirstOrDefault()?.Item2 ?? TokenType.NONE;
			} else if (Tok.StartsWith("\"") && Tok.EndsWith("\"")) {
				T.Tok = TokenType.STRING;
			} else if (T.Tok == TokenType.NUMBER) {
				T.NumType = NumberType.DEC;

				if (Tok.Contains("0x"))
					T.NumType = NumberType.HEX;
				else if (Tok.Contains("0o"))
					T.NumType = NumberType.OCT;
				else if (Tok.Contains("0b"))
					T.NumType = NumberType.BIN;
				else if (Tok.Contains("."))
					T.NumType = NumberType.FLOAT;

			} else if (T.Tok == TokenType.NONE) {
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
				Fail("Internal error, unknown token type {0}", T);

			Tokens.Add(T);
		}
	}

	struct Token {
		public string Src;
		public TokenType Tok;
		public NumberType NumType;

		public int Line;
		public int Col;

		public Token(string Src, TokenType Tok, int Line, int Col) {
			this.Src = Src;
			this.Tok = Tok;
			this.Line = Line;
			this.Col = Col;
			NumType = NumberType.NONE;
		}

		public override string ToString() {
			string TokType = Tok.ToString();

			if (Tok == TokenType.NUMBER) {
				TokType += string.Format(" {0}", NumType);
			}

			string TokName = string.Format("{0}:{1}:{2} ", TokType, Line, Col);
			int TokLen = 24;

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

	enum NumberType {
		NONE,
		DEC,
		HEX,
		OCT,
		BIN,
		FLOAT,
	}
}
