using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TokenTuple = System.Tuple<string, FishMarkupLanguage.TokenType>;

namespace FishMarkupLanguage {
	public static class FML {
		static int Line;
		static int Col;

		static Dictionary<string, TokenType> SymbolTypes = new Dictionary<string, TokenType>() {
			{ "$", TokenType.DOLLAR },
			{ "#", TokenType.HASH },
			{ "{", TokenType.BRACKET_OPEN },
			{ "}", TokenType.BRACKET_CLOSE },
			{ "=", TokenType.EQUAL },
			{ ";", TokenType.SEMICOLON },
			{ ".", TokenType.DOT },
		};

		static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>() {
			{"template", TokenType.IDENTIFIER},
			{"none", TokenType.IDENTIFIER},

			{"true", TokenType.IDENTIFIER},
			{"false", TokenType.IDENTIFIER}
		};

		static bool IsSymbol(char C) {
			if (SymbolTypes.ContainsKey(C.ToString()))
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

		static IEnumerable<Token> Lex(string FileName) {
			string Source = "";
			int Retries = 0;

			while (true) {
				try {
					Source = File.ReadAllText(FileName).Replace("\r", "") + "\n";
					break;
				} catch (Exception) {
					Thread.Sleep(50);
					Retries++;

					if (Retries >= 6)
						throw;
				}
			}

			bool InQuote = false;
			bool InSLComment = false;
			bool InMLComment = false;
			bool InNumber = false;
			bool InDocument = false;
			int DocEqLen = -1;

			StringBuilder CurTok = new StringBuilder();
			//List<Token> Tokens = new List<Token>();

			Func<TokenType, Token?> EmitIfNotEmpty = (TT) => {
				if (CurTok.Length > 0) {
					Token Ret = EmitToken(CurTok.ToString(), TT);
					CurTok.Clear();
					return Ret;
				}

				return null;
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

						Token? T = EmitIfNotEmpty(TokenType.NUMBER);
						if (T != null)
							yield return T.Value;
					}
				}

				Col++;
				if (IsNewLine(C)) {
					Col = 0;
					Line++;
				}

				if (!InMLComment && !InDocument && !InQuote) {
					if (C == '/' && NC == '/') {
						Token? T = EmitIfNotEmpty(TokenType.NONE);
						if (T != null)
							yield return T.Value;

						InSLComment = true;
					}

					if (InSLComment) {
						if (IsNewLine(C)) {
							InSLComment = false;

							Token? T = EmitIfNotEmpty(TokenType.NONE);
							if (T != null)
								yield return T.Value;
						}

						continue;
					}
				}

				if (!InSLComment && !InDocument) {
					if (C == '/' && NC == '*') {
						Token? T = EmitIfNotEmpty(TokenType.NONE);
						if (T != null)
							yield return T.Value;

						InMLComment = true;
					}

					if (InMLComment) {
						if (C == '*' && NC == '/') {
							i++;
							InMLComment = false;

							Token? T = EmitIfNotEmpty(TokenType.NONE);
							if (T != null)
								yield return T.Value;
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
							yield return new Token(CurTok.ToString(), TokenType.DOCUMENT, Line, Col);

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
					Token? T = EmitIfNotEmpty(TokenType.NONE);
					if (T != null)
						yield return T.Value;

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

				if (InQuote && C == '\\' && NC == '"') {
					continue;
				}

				if (C == '\"') {
					if (!InQuote) {
						InQuote = true;

						if (CurTok.Length > 0)
							yield return EmitToken(CurTok.ToString());

						CurTok.Clear();
					} else if (PC != '\\' || (PC == '\\' && PPC == '\\')) {
						InQuote = false;

						yield return EmitToken("\"" + CurTok.ToString() + "\"");

						CurTok.Clear();
					} else
						CurTok.Append(C);
					continue;
				}

				if (InQuote) {
					CurTok.Append(C);
					continue;
				}

				if (IsWhiteSpace(C)) {
					Token? T = EmitIfNotEmpty(TokenType.NONE);
					if (T != null)
						yield return T.Value;
				} else if (CurTok.Length == 0 && CanNumStartWith(C, NC)) {
					InNumber = true;
					CurTok.Append(C);
				} else if (IsSymbol(C)) {
					/*if (CurTok.Length == 0 && CanNumStartWith(C, NC)) {
						InNumber = true;
						CurTok.Append(C);
						continue;
					}*/

					Token? T = EmitIfNotEmpty(TokenType.NONE);
					if (T != null)
						yield return T.Value;

					yield return EmitToken(C.ToString());
				} else {
					// Numbers
					/*if (CurTok.Length == 0 && CanNumStartWith(C, NC))
						InNumber = true;*/

					CurTok.Append(C);
				}
			}

			if (CurTok.Length > 0)
				yield return EmitToken(CurTok.ToString());
		}

		public static void Parse(string FileName, FMLDocument Doc) {
			Token[] Tokens = Lex(FileName).ToArray();
			List<FMLTag> Tags = new List<FMLTag>();

			for (int i = 0; i < Tokens.Length;) {
				if (TryParseTag(ref i, Tokens, Doc, null, out FMLTag T)) {
					if (T != null)
						Tags.Add(T);
				} else
					throw new Exception("Invalid token\n" + Tokens[i]);
			}

			Doc.Tags.Clear();
			Doc.Tags.AddRange(Tags);
		}

		public static FMLDocument Parse(string FileName) {
			FMLDocument Doc = new FMLDocument();
			Doc.TagSet.AnyTagValid = true;

			Parse(FileName, Doc);

			return Doc;
		}

		static object ParseValueToken(Token T) {
			switch (T.Tok) {
				case TokenType.BOOL: {
						if (T.Src == "false")
							return false;
						else if (T.Src == "true")
							return true;
						else
							throw new NotImplementedException();
					}

				case TokenType.NUMBER:
					switch (T.NumType) {
						case NumberType.DEC:
							return int.Parse(T.Src);

						case NumberType.FLOAT: {
								string FloatSrc = T.Src;

								if (FloatSrc.EndsWith("f"))
									FloatSrc = FloatSrc.Substring(0, FloatSrc.Length - 1);

								return float.Parse(FloatSrc, CultureInfo.InvariantCulture);
							}

						default:
							throw new Exception("Could not parse number " + T.Src);
					}

				case TokenType.STRING:
					// TODO: Make it better
					return T.Src.Substring(1, T.Src.Length - 2).Replace("\\n", "\n");

				case TokenType.DOCUMENT:
					return new FMLHereDoc(T.Src);

				default:
					throw new InvalidOperationException();
			}
		}

		static bool TryParseTag(ref int i, Token[] Tokens, FMLDocument Doc, FMLTemplateTag RootTemplateTag, out FMLTag Tag) {
			//Tag = null;

			if (Tokens[i].Tok == TokenType.IDENTIFIER && Tokens[i].Src == "template") {
				if (RootTemplateTag != null)
					throw new Exception("Invalid nesting of template tags");

				FMLTemplateTag TemplateTag = new FMLTemplateTag();
				Doc.Templates.Add(TemplateTag);
				i++;

				if (Tokens[i].Tok != TokenType.IDENTIFIER)
					throw new Exception("Expected template name");

				TemplateTag.TemplateName = Tokens[i].Src;
				i++;

				while (Tokens[i].Tok == TokenType.IDENTIFIER) {
					string AttribName = Tokens[i].Src;
					object Value = null;

					if (Tokens[i + 1].Tok == TokenType.EQUAL) {
						Value = ParseValueToken(Tokens[i + 2]);
						i += 2;
					}

					TemplateTag.Attributes.SetAttribute(AttribName, Value);
					i++;
				}

				if (Tokens[i].Tok == TokenType.BRACKET_OPEN) {
					i++;

					while (Tokens[i].Tok != TokenType.BRACKET_CLOSE) {
						if (TryParseTag(ref i, Tokens, Doc, TemplateTag, out FMLTag NewTag))
							TemplateTag.AddChild(NewTag);
						else
							throw new Exception("Invalid token\n" + Tokens[i]);
					}

					i++;
					Tag = null;
					return true;
				} else if (Tokens[i].Tok == TokenType.SEMICOLON) {
					i++;
					Tag = null;
					return true;
				}
			} else if (Tokens[i].Tok == TokenType.IDENTIFIER && Doc.TagSet.IsValid(Tokens[i].Src)) {
				Tag = new FMLTag(Tokens[i].Src);
				i++;

				while (Tokens[i].Tok == TokenType.IDENTIFIER) {
					string AttribName = Tokens[i].Src;
					object Value = true;

					if (Tokens[i + 1].Tok == TokenType.EQUAL) {
						if (Tokens[i + 2].Tok == TokenType.DOLLAR) {
							i += 3;

							Value = new FMLTemplateValue(Tokens[i].Src);
						} else {
							Value = ParseValueToken(Tokens[i + 2]);
							i += 2;
						}
					}

					Tag.Attributes.SetAttribute(AttribName, Value);
					i++;
				}

				if (Tokens[i].Tok == TokenType.BRACKET_OPEN) {
					i++;

					while (Tokens[i].Tok != TokenType.BRACKET_CLOSE) {
						if (TryParseTag(ref i, Tokens, Doc, RootTemplateTag, out FMLTag NewTag))
							Tag.AddChild(NewTag);
						else
							throw new Exception("Invalid token");
					}

					i++;
					return true;
				} else if (Tokens[i].Tok == TokenType.SEMICOLON) {
					i++;
					return true;
				}
			} else if (Tokens[i].IsValueToken()) {
				Tag = new FMLValueTag(ParseValueToken(Tokens[i]));

				if (Tokens[i + 1].Tok != TokenType.SEMICOLON)
					throw new Exception("Expected semicolon");

				i += 2;
				return true;
			} else if (Tokens[i].Tok == TokenType.DOLLAR && Tokens[i + 1].Tok == TokenType.IDENTIFIER) {
				if (RootTemplateTag == null)
					throw new Exception("Invalid use of $ outside of templates");
				i++;

				if (Tokens[i].Tok != TokenType.IDENTIFIER)
					throw new Exception("Expected identifier");

				FMLTemplateValueTag ValTag = new FMLTemplateValueTag(RootTemplateTag);
				Tag = ValTag;
				ValTag.TagName = Tokens[i].Src;
				i++;

				if (Tokens[i].Tok != TokenType.SEMICOLON)
					throw new Exception("Expected semicolon");

				i++;
				return true;
			}

			Tag = null;
			return false;
		}

		static Token EmitToken(string Tok, TokenType TokType = TokenType.NONE) {
			Token T = new Token(Tok, TokType, Line, Col);

			if (Tok.Length == 1 && IsSymbol(Tok[0])) {
				T.Tok = SymbolTypes[Tok];
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
					if (Keywords.ContainsKey(Tok)) {
						T.Tok = Keywords[Tok];
					} else
						T.Tok = TokenType.IDENTIFIER;
				}
			}

			if (T.Tok == TokenType.NONE)
				Fail("Internal error, unknown token type {0}", T);

			return T;
		}
	}
}
