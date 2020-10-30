﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ABJAD.Lexer
{
    public class Lexer
    {
        private static readonly string DigitRegex = "[٠-٩]";
        private static readonly string NumberRegex = @"[٠-٩]*(\.[٠-٩]*)?";
        private static readonly string LiteralRegex = @"";
        private static readonly string WordTerminalRegex = @"[();{} !@#$%&*-+=.,/\`~'"":\[\]\?\^]";

        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            { "أيضا", TokenType.AND },
            { "ايضا", TokenType.AND },
            { "صنف", TokenType.CLASS },
            { "ثابت", TokenType.CONST },
            { "غيره", TokenType.ELSE },
            { "خطأ", TokenType.FALSE },
            { "خطا", TokenType.FALSE },
            { "بـ", TokenType.FOR },
            { "وظيفة", TokenType.FUNC },
            { "إذا", TokenType.IF },
            { "اذا", TokenType.IF },
            { "إنشاء", TokenType.NEW },
            { "عدم", TokenType.NULL },
            { "أو", TokenType.OR },
            { "او", TokenType.OR },
            { "أكتب", TokenType.PRINT },
            { "اكتب", TokenType.PRINT },
            { "أرجع", TokenType.RETURN },
            { "ارجع", TokenType.RETURN },
            { "صحيح", TokenType.TRUE },
            { "طالما", TokenType.WHILE },
            { "متغير", TokenType.VAR },
        };

        private readonly string code;

        private List<Token> Tokens;

        private int _line = 1;
        private int _lineIndex = 0;
        private int _current = 0;

        public Lexer(string fileName)
        {
            // TODO assign code to reader.read
        }

        public List<Token> Lex()
        {
            Tokens = new List<Token>();
            char c;
            while (HasNext(out c))
            {
                ScanToken(c);
            }

            return Tokens;
        }

        private bool HasNext(out char c)
        {
            if (_current >= code.Length)
            {
                c = default;
                return false;
            }

            c = code[_current];
            IncrementIndex(1);
            return true;
        }

        private string NextWord()
        {
            int currentCopy = _current - 1;
            char currentChar;
            var wordBuilder = new StringBuilder();

            while (!Regex.IsMatch((currentChar = code[currentCopy++]).ToString(), WordTerminalRegex))
            {
                wordBuilder.Append(currentChar);
            }

            return wordBuilder.ToString();
        }

        private bool IsNext(char expected, out char next)
        {
            if (!HasNext(out next))
            {
                //TODO throw new AbjadExpectedTokenNotFoundException(_line, expected.ToString());
            }

            if (next == expected)
            {
                return true;
            }

            return false;
        }

        private void IncrementIndex(int num)
        {
            _current += num;
            _lineIndex += num;
        }

        private void DecrementIndex(int num)
        {
            _current -= num;
            _lineIndex -= num;
        }

        private void ScanToken(char c)
        {
            switch (c)
            {
                case '\t': return;
                case '\r': return;
                case ' ': return; // skip white spaces
                case '\n': _line++; _lineIndex = 0; return;
                case '؛': AddToken(TokenType.SEMICOLON); return;
                case '،': AddToken(TokenType.COMMA); return;
                case '.': AddToken(TokenType.DOT); return;
                case '{': AddToken(TokenType.OPEN_BRACE); return;
                case '}': AddToken(TokenType.CLOSE_BRACE); return;
                case '(': AddToken(TokenType.OPEN_PAREN); return;
                case ')': AddToken(TokenType.CLOSE_PAREN); return;
                case '+': AddToken(TokenType.PLUS); return;
                case '*': AddToken(TokenType.TIMES); return;
                case '-':
                    if (Match(DigitRegex))
                    {
                        DecrementIndex(1);

                        // check for negative numbers
                        if (!ScanNumber())
                        {
                            // TODO throw new AbjadUnexpectedTokenException(_line, _lineIndex);
                        }
                    }
                    else
                    {
                        AddToken(TokenType.MINUS);
                    }
                    return;
                case '"':
                    var str = new StringBuilder();
                    while (!IsNext('"', out char next))
                    {
                        str.Append(next);
                    }
                    AddToken(TokenType.STRING_CONST, str.ToString());
                    return;
                case '\\':
                    if (Match('\\'))
                    {
                        while (Peek() != '\n') continue;

                        DecrementIndex(1);
                    }
                    else
                    {
                        AddToken(TokenType.DIVIDED_BY);
                    }
                    return;
                case '=':
                    if (Match('='))
                        AddToken(TokenType.EQUAL_EQUAL);
                    else
                        AddToken(TokenType.EQUAL);
                    return;
                case '!':
                    if (Match('='))
                        AddToken(TokenType.BANG_EQUAL);
                    else
                        AddToken(TokenType.BANG);
                    return;
                case '>':
                    if (Match('='))
                        AddToken(TokenType.LESS_EQUAL);
                    else
                        AddToken(TokenType.LESS_THAN);
                    return;
                case '<':
                    if (Match('='))
                        AddToken(TokenType.GREATER_EQUAL);
                    else
                        AddToken(TokenType.GREATER_THAN);
                    return;
                default:
                    if (ScanKeyword()) return;
                    if (ScanNumber()) return;
                    if (ScanLiteral()) return;
                    // TODO throw new AbjadUndefinedTokenException(_line, _lineIndex, c.ToString());
            }
        }

        private void AddToken(TokenType type)
        {
            Tokens.Add(new Token(type, code[_current - 1]));
        }

        private void AddToken(TokenType type, string text)
        {
            Tokens.Add(new Token(type, text));
        }

        private bool ScanLiteral()
        {
            var word = NextWord();
            if (Regex.IsMatch(word, LiteralRegex))
            {
                IncrementIndex(word.Length - 1);
                AddToken(TokenType.ID, word);
                return true;
            }

            return false;
        }

        private bool ScanNumber()
        {
            var word = NextWord();
            if (Regex.IsMatch(word, NumberRegex))
            {
                IncrementIndex(word.Length - 1);
                AddToken(TokenType.NUMBER_CONST, word);
                return true;
            }

            return false;
        }

        private bool ScanKeyword()
        {
            return MatchKeyword(NextWord());
        }

        private bool MatchKeyword(string word)
        {
            if (Keywords.ContainsKey(word))
            {
                IncrementIndex(word.Length - 1);
                AddToken(Keywords[word], word);
                return true;
            }

            return false;
        }

        private bool Match(char expected)
        {
            if (HasNext(out char c))
            {
                if (c == expected)
                {
                    return true;
                }

                DecrementIndex(1);
            }

            return false;
        }

        private bool Match(string regex)
        {
            if (HasNext(out char c))
            {
                if (Regex.IsMatch(c.ToString(), regex))
                {
                    return true;
                }

                DecrementIndex(1);
            }

            return false;
        }

        private char Peek()
        {
            var c = code[_current];
            IncrementIndex(1);
            return c;
        }
    }
}