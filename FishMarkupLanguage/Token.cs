using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FishMarkupLanguage {
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

		public bool IsValueToken() {
			if (Tok == TokenType.NUMBER || Tok == TokenType.STRING || Tok == TokenType.DOCUMENT)
				return true;

			return false;
		}
	}

	enum TokenType {
		NONE,

		IDENTIFIER,
		NUMBER,
		STRING,
		BOOL,
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
