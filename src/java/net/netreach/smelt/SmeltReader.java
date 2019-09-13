///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.SmeltReader
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/17
// Description: Parse a Smelt file and output a stream of tokens.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool (runtime libraries).
// Copyright (c) 2000-2001 NetReach, Inc.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307  USA
//
// The GNU Lesser General Public License should be located in the file
// COPYING.lgpl. For more information on the specific license terms for nrdo,
// please see the file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.smelt;

// I/O classes used.
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.io.Reader;

import net.netreach.util.FileLocation;

public class SmeltReader {

  /**
   * Set to true to enable debugging printouts of all tokens parsed.
   */
  private static final boolean DEBUG = false;

  /**
   * Returned by readToken if a start-of-block '{' token is encountered.
   */
  public static final Object SOB = new Object();

  /**
   * Returned by readToken if an end-of-block '}' token is encountered.
   */
  public static final Object EOB = new Object();

  /**
   * Returned by readToken if an end-of-line ';' token is encountered. An EOL
   * token is guaranteed to be returned before an EOB or EOF token, even if no
   * ';' is explicitly present in the input, unless the block is completely
   * empty.
   */
  public static final Object EOL = new Object();

  /**
   * Returned by readToken if an end-of-file is encountered.
   */
  public static final Object EOF = new Object();

  /**
   * Create a SmeltReader based on a file. Equivalent to SmeltReader(new
   * FileReader(f), f.toString()).
   * 
   * @param f
   *          the smelt file to interpret.
   */
  public SmeltReader(File f) throws IOException, ParseException {
    this(new FileReader(f), f);
  }

  /**
   * Create a SmeltReader based on a stream of characters.
   * 
   * @param in
   *          the stream of characters to interpret.
   * @param filename
   *          the name to associate with this stream in error messages.
   */
  public SmeltReader(Reader in, File filename) throws ParseException {
    this.in = new LineColReader(in);
    this.filename = filename;
    readChar();
  }
  
  public SmeltReader(Reader in, String filename) throws ParseException {
    this(in, new File(filename));
  }

  /**
   * Create a SmeltReader without any other parameters. This is designed to be
   * used by subclasses only, because readToken() (and/or all the other readFoo
   * methods) will fail catastrophically if no Reader is present. The subclass
   * must provide useful implementations of readToken() or readFoo for all
   * relevant Foo in order to function if this constructor is used.
   */
  protected SmeltReader() {
  }

  /**
   * Read a single token from the smelt stream.
   * 
   * @return EOF if the end of file was encountered, SOB if an open-brace '{'
   *         was encountered, EOB if a close-brace '}' was encountered, EOL if a
   *         semicolon ';' was encountered, or a String object containing the
   *         single word that was encountered. An EOL is guaranteed to be
   *         returned before every EOB or EOF, unless the block is completely
   *         empty.
   * @exception ParseException
   *              if mismatched braces, an unterminated string, or any other raw
   *              syntactical error was found in the stream.
   */
  public Object readToken() throws ParseException {
    readWhitespace();
    boolean readNext = true;
    switch (ch) {
    case -1:
      if (nestingDepth > 0) {
        throw new ParseException(this, "End of file reached with "
            + nestingDepth + " block(s) unclosed");
      }
      tokenStartRow = tokenEndRow = in.getLineNumber() + 1;
      tokenStartCol = tokenEndCol = in.getColumnNumber();
      if (lastToken == EOL || lastToken == null) {
        lastToken = EOF;
      } else {
        lastToken = EOL;
      }
      readNext = false;
      break;
    case ';':
      tokenStartRow = tokenEndRow = in.getLineNumber() + 1;
      tokenStartCol = tokenEndCol = in.getColumnNumber();
      lastToken = EOL;
      break;
    case '{':
      tokenStartRow = tokenEndRow = in.getLineNumber() + 1;
      tokenStartCol = tokenEndCol = in.getColumnNumber();
      nestingDepth++;
      lastToken = SOB;
      break;
    case '}':
      tokenStartRow = tokenEndRow = in.getLineNumber() + 1;
      tokenStartCol = tokenEndCol = in.getColumnNumber();
      if (nestingDepth <= 0) {
        throw new ParseException(this, "Unmatched end-of-block ('}') found");
      }
      if (lastToken == EOL || lastToken == SOB) {
        nestingDepth--;
        lastToken = EOB;
      } else {
        readNext = false;
        lastToken = EOL;
      }
      break;
    case '[':
      // readLiteral() handles setting the start/end-row/cols for us.
      lastToken = readLiteral();
      readNext = false;
      break;
    default:
      // readWord() handles setting the start/end-row/cols for us.
      lastToken = readWord();
      readNext = false;
      break;
    }
    if (readNext) readChar();
    if (DEBUG) System.err.println("SR:  Read token: " + lastToString());
    return lastToken;
  }

  /**
   * Read a token and check that it's equal to a specific value. Equivalent to
   * readToken() followed by checkToken(expected).
   * 
   * @param expected
   *          the token that is expected to be returned. This must be either a
   *          string value (which will be compared exactly to the returned
   *          string) or one of SOB, EOB, EOL or EOF.
   * @exception ParseException
   *              if anything other than the expected value was encountered, or
   *              a raw syntactical error was found in the stream.
   */
  public void readToken(Object expected) throws ParseException {
    readToken();
    checkToken(expected);
  }

  /**
   * Check that the last-read token was equal to a specific value, then read
   * another token. Equivalent to checkToken(expected) followed by readToken().
   * 
   * @param expected
   *          the token that is expected to be the last token read.
   * @exception ParseException
   *              if anything other than the expected value was found, or a raw
   *              syntactical error was found in the stream.
   */
  public void skipToken(Object expected) throws ParseException {
    checkToken(expected);
    readToken();
  }

  /**
   * Test whether the last-read token was equal to a specific value.
   * 
   * @param expected
   *          the token that is expected in the file. This must be either a
   *          string value (which will be compared exactly to the returned
   *          string) or one of SOB, EOB, EOL or EOF.
   * @return true if the last-read token was equal to expected, false otherwise.
   */
  public boolean wasToken(Object expected) {
    return lastToken.equals(expected);
  }

  /**
   * Check that the last-read token was equal to a specific value.
   * 
   * @param expected
   *          the token that is expected to be returned. This must be either a
   *          string value (which will be compared exactly to the returned
   *          string) or one of SOB, EOB, EOL or EOF.
   * @exception ParseException
   *              if anything other than the expected value was encountered.
   */
  public void checkToken(Object expected) throws ParseException {
    if (!wasToken(expected)) {
      throw new ParseException(this, "Expected " + toString(expected)
          + ", found " + lastToString());
    }
  }

  /**
   * Read a string from the smelt stream.
   * 
   * @return the string that was read.
   * @exception ParseException
   *              if anything other than a string (ie an SOB, EOB, EOL or EOF)
   *              was encountered, or a raw syntactical error was found in the
   *              stream.
   */
  public String readString() throws ParseException {
    readToken();
    return checkString();
  }

  /**
   * Test whether the last encountered token was a String.
   * 
   * @return true if the last encountered token was a string, false otherwise.
   */
  public boolean wasString() {
    return lastToken instanceof String;
  }

  /**
   * Check that the last encountered token was a String, and return it.
   * 
   * @return the string that was read.
   * @exception ParseException
   *              if the last token read was anything other than a string (ie an
   *              SOB, EOB, EOL or EOF).
   */
  public String checkString() throws ParseException {
    if (wasString()) return (String) lastToken;
    throw new ParseException(this, "Expected a string, found " + lastToString());
  }

  /**
   * Check that the last encountered token was a String, and read another token.
   * 
   * @return the string that was the last encountered token.
   * @exception ParseException
   *              if the last token read was anything other than a string (ie an
   *              SOB, EOB, EOL or EOF), or if a raw syntactical error was
   *              encountered in the file.
   */
  public String skipString() throws ParseException {
    String result = checkString();
    readToken();
    return result;
  }

  /**
   * Read a start-of-block character '{' from the stream. Equivalent to
   * readToken(SOB).
   * 
   * @exception ParseException
   *              if anything other than an SOB (ie an EOB, EOL, EOF or string)
   *              was encountered, or a raw syntactical error was found in the
   *              stream.
   */
  public void readSOB() throws ParseException {
    readToken(SOB);
  }

  /**
   * Read an end-of-block character '}' from the stream. Equivalent to
   * readToken(EOB).
   * 
   * @exception ParseException
   *              if anything other than an EOB (ie an SOB, EOL, EOF or string)
   *              was encountered, or a raw syntactical error was found in the
   *              stream.
   */
  public void readEOB() throws ParseException {
    readToken(EOB);
  }

  /**
   * Read an end-of-line character ';' from the stream. If a close-brace or EOF
   * is encountered without a semicolon coming immediately before, the EOL token
   * is implied, unless the block is completely empty. Equivalent to
   * readToken(EOL).
   * 
   * @exception ParseException
   *              if anything other than an EOL (ie an SOB, EOB, EOF or string)
   *              was encountered, or a raw syntactical error was found in the
   *              stream.
   */
  public void readEOL() throws ParseException {
    readToken(EOL);
  }

  /**
   * Read an end-of-file token from the stream. In other words, verify that the
   * end of the stream has been reached. Equivalent to readToken(EOF).
   * 
   * @exception ParseException
   *              if anything other than an EOF (ie an SOB, EOB, EOL or string)
   *              was encountered, or a raw syntactical error was found in the
   *              stream.
   */
  public void readEOF() throws ParseException {
    readToken(EOF);
  }

  /**
   * Check that the last token read was a start-of-block, then read another
   * token. Equivalent to skipToken(SOB).
   * 
   * @exception ParseException
   *              if the last token was anything other than an SOB (ie an EOB,
   *              EOL, EOF or string) or a raw syntactical error was encountered
   *              in the file.
   */
  public void skipSOB() throws ParseException {
    skipToken(SOB);
  }

  /**
   * Check that the last token read was a end-of-block, then read another token.
   * Equivalent to skipToken(EOB).
   * 
   * @exception ParseException
   *              if the last token was anything other than an EOB (ie an SOB,
   *              EOL, EOF or string) or a raw syntactical error was encountered
   *              in the file.
   */
  public void skipEOB() throws ParseException {
    skipToken(EOB);
  }

  /**
   * Check that the last token read was a end-of-line, then read another token.
   * Equivalent to skipToken(EOL).
   * 
   * @exception ParseException
   *              if the last token was anything other than an EOL (ie an SOB,
   *              EOB, EOF or string) or a raw syntactical error was encountered
   *              in the file.
   */
  public void skipEOL() throws ParseException {
    skipToken(EOL);
  }

  /**
   * Test whether the last token read was an SOB.
   * 
   * @return true if the last token read was an SOB, false otherwise.
   */
  public boolean wasSOB() {
    return wasToken(SOB);
  }

  /**
   * Test whether the last token read was an EOB.
   * 
   * @return true if the last token read was an EOB, false otherwise.
   */
  public boolean wasEOB() {
    return wasToken(EOB);
  }

  /**
   * Test whether the last token read was an EOL.
   * 
   * @return true if the last token read was an EOL, false otherwise.
   */
  public boolean wasEOL() {
    return wasToken(EOL);
  }

  /**
   * Test whether the last token read was an EOF.
   * 
   * @return true if the last token read was an EOF, false otherwise.
   */
  public boolean wasEOF() {
    return wasToken(EOF);
  }

  /**
   * Check that the last token read was a start-of-block '{'. Equivalent to
   * checkToken(SOB).
   * 
   * @exception ParseException
   *              if the last token was anything other than an SOB (ie an EOB,
   *              EOL, EOF or string).
   */
  public void checkSOB() throws ParseException {
    checkToken(SOB);
  }

  /**
   * Check that the last token read was an end-of-block '}'. Equivalent to
   * checkToken(EOB).
   * 
   * @exception ParseException
   *              if the last token was anything other than an EOB (ie an SOB,
   *              EOL, EOF or string).
   */
  public void checkEOB() throws ParseException {
    checkToken(EOB);
  }

  /**
   * Check that the last token read was an end-of-line ';'. Equivalent to
   * checkToken(EOL).
   * 
   * @exception ParseException
   *              if the last token was anything other than an EOL (ie an SOB,
   *              EOB, EOF or string).
   */
  public void checkEOL() throws ParseException {
    checkToken(EOL);
  }

  /**
   * Check that the last token read was an end-of-file. Equivalent to
   * checkToken(EOF).
   * 
   * @exception ParseException
   *              if the last token was anything other than an EOF (ie an SOB,
   *              EOB, EOL or string).
   */
  public void checkEOF() throws ParseException {
    checkToken(EOF);
  }

  /**
   * Get the last token that was read from the file.
   */
  public Object getLastToken() {
    return lastToken;
  }

  /**
   * Get the line number in the file for the character at a particular position
   * in the previously-read token.
   * 
   * @param pos
   *          the position in the string to read.
   * @exception IllegalArgumentException
   *              if the token is not a string.
   * @return the line number of the character in position pos in the most
   *         recently read token.
   */
  private int getRowForCharAt(int pos) {
    if (!(lastToken instanceof String)) {
      throw new IllegalArgumentException("Can't get positions in non-string");
    }
    String s = (String) lastToken;
    if (pos < 0 || pos > s.length()) {
      throw new IndexOutOfBoundsException("" + pos);
    }
    int result = tokenStartRow - 1;
    while (pos != -1) {
      result++;
      pos--;
      if (pos >= 0) pos = s.lastIndexOf('\n', pos);
    }
    return result;
  }

  /**
   * Get the column in the file for the character at a particular position in
   * the previously-read token.
   * 
   * @param pos
   *          the position in the string to read.
   * @exception IllegalArgumentException
   *              if the token is not a string.
   * @return the column of the character in position pos in the most recently
   *         read token.
   */
  private int getColForCharAt(int pos) {
    if (!(lastToken instanceof String)) {
      throw new IllegalArgumentException("Can't get positions in non-string");
    }
    String s = (String) lastToken;
    if (pos < 0 || pos > s.length()) {
      throw new IndexOutOfBoundsException("" + pos);
    }
    int lstart = s.lastIndexOf('\n', pos);
    int result;
    String resultLine;
    if (lstart == -1) {
      resultLine = s.substring(0, pos);
      result = tokenStartCol + pos;
    } else {
      resultLine = s.substring(lstart, pos);
      result = pos - lstart - 1;
    }

    // Correct for escaped characters ('[[' and '[]') if the token was a
    // bracketed string. We can do this unconditionally because if the token
    // wasn't a bracketed string, [ and ] aren't legal characters.
    for (int i = 0; i < resultLine.length(); i++) {
      char ch = resultLine.charAt(i);
      if (ch == '[' || ch == ']') result++;
    }
    return result;
  }
  
  public FileLocation getLocForStringChar(int pos) {
    int line = this.getRowForCharAt(pos);
    int col = this.getColForCharAt(pos);
    return new FileLocation(filename, line, col);
  }
  public FileLocation getLocForStringRange(int startPos, int endPos) {
    int line = this.getRowForCharAt(startPos);
    int col = this.getColForCharAt(startPos);
    int endLine = this.getRowForCharAt(endPos);
    int endCol = this.getColForCharAt(endPos);
    return new FileLocation(filename, line, col, endLine, endCol);
  }

  private LineColReader in;
  File filename;
  protected Object lastToken = null;
  private int tokenStartRow = 0;
  private int tokenStartCol = 0;
  private int tokenEndRow = 0;
  private int tokenEndCol = 0;
  private int nestingDepth = 0;

  /**
   * Convert a token into a string that would be usable in error messages.
   */
  public static String toString(Object tok) {
    if (tok instanceof String) {
      String s = (String) tok;
      int fline = s.indexOf('\n');
      int lline = s.lastIndexOf('\n');
      int len = s.length();
      if (fline == -1 || fline > 5) fline = 5;
      if (lline == -1 || lline < len - 5) lline = len - 5;
      String summary = lline <= fline ? s : s.substring(0, fline) + "..."
          + s.substring(lline);
      return "a string (\"" + summary + "\")";
    } else if (tok == EOF) {
      return "end-of-file";
    } else if (tok == EOL) {
      return "end of line (';' or '}')";
    } else if (tok == SOB) {
      return "start of block ('{')";
    } else if (tok == EOB) {
      return "end of block ('}')";
    } else {
      throw new RuntimeException("Unknown token type passed to toString(tok)");
    }
  }

  /**
   * Convert the last-read token into a string that would be useful in error
   * messages. Equivalent to toString(getLastToken()).
   */
  public String lastToString() {
    return toString(lastToken);
  }

  private int ch;

  private void readChar() throws ParseException {
    try {
      if (ch != -1) {
        ch = in.read();
        if (ch == -1) {
          in.close();
        }
      }
    } catch (IOException e) {
      throw new ParseException(this, "I/O Error: " + e);
    }
  }

  private void readWhitespace() throws ParseException {
    boolean inComment = ch == '#';
    if (DEBUG) if (inComment) System.err.print("SR:  Comment: ");
    while (inComment || Character.isWhitespace((char) ch)) {
      if (DEBUG) if (inComment) System.err.print((char) ch);
      readChar();
      if (inComment && (ch == '\n' || ch == '\r')) {
        inComment = false;
        if (DEBUG) System.err.println();
      }
      if (ch == '#' && !inComment) {
        inComment = true;
        if (DEBUG) System.err.print("SR:  Comment: ");
      }
    }
  }

  private String readLiteral() throws ParseException {
    if (ch != '[') throw new RuntimeException("readLiteral called w/ ch != [");
    readChar();
    tokenStartRow = in.getLineNumber() + 1;
    tokenStartCol = in.getColumnNumber();
    StringBuffer sb = new StringBuffer();
    while (ch != ']') {

      // The '[' character is used to escape ']' characters in the string.
      switch (ch) {
      case '[':
        readChar();
        if (ch != '[' && ch != ']') {
          tokenEndRow = in.getLineNumber() + 1;
          tokenEndCol = in.getColumnNumber();
          throw new ParseException(this, "Illegal escaped character '"
              + (char) ch + "'", true);
        }
        sb.append((char) ch);

        // Change "ch" so as not to break out of the while loop.
        if (ch == ']') ch = '[';
        break;
      case -1:
        tokenEndRow = in.getLineNumber() + 1;
        tokenEndCol = in.getColumnNumber();
        throw new ParseException(this, "Unterminated string literal");
      default:
        sb.append((char) ch);
        break;
      }
      readChar();
    }
    tokenEndRow = in.getLineNumber() + 1;
    tokenEndCol = in.getColumnNumber();
    readChar(); // Skip the ']'
    return sb.toString();
  }

  private String readWord() throws ParseException {
    tokenStartRow = in.getLineNumber() + 1;
    tokenStartCol = in.getColumnNumber();
    StringBuffer sb = new StringBuffer();
    while (ch != -1 && ch != ';' && ch != '{' && ch != '}' && ch != '#'
        && !Character.isWhitespace((char) ch)) {
      if (ch == '[' || ch == ']') {
        tokenEndRow = in.getLineNumber() + 1;
        tokenEndCol = in.getColumnNumber();
        throw new ParseException(this,
            "Illegal character: '" + (char) ch + "'", true);
      }
      sb.append((char) ch);
      readChar();
    }
    tokenEndRow = in.getLineNumber() + 1;
    tokenEndCol = in.getColumnNumber();
    return sb.toString();
  }

  /**
   * Run this class with DEBUG (above) set to true and pass one or more smelt
   * filenames on the command line to generate a dump of the tokens in the file.
   */
  public static void main(String[] args) throws ParseException, IOException {
    for (int i = 0; i < args.length; i++) {
      SmeltReader sr = new SmeltReader(new File(args[i]));
      while (sr.readToken() != EOF)
        ;
    }
  }

  public FileLocation getLastTokenLocation() {
    return new FileLocation(filename, tokenStartRow, tokenStartCol, tokenEndRow, tokenEndCol);
  }
  
  public FileLocation getLastTokenEndLocation() {
    return new FileLocation(filename, tokenEndRow, tokenEndCol);
  }
}
