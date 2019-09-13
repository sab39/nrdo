package net.netreach.util;

import java.io.File;

public class FileLocation {
  private final File file;
  public File getFile() {
    return file;
  }
  private final int line;
  public int getLine() {
    return line;
  }
  private final int col;
  public int getCol() {
    return col;
  }
  private final int endLine;
  public int getEndLine() {
    return endLine;
  }
  private final int endCol;
  public int getEndCol() {
    return endCol;
  }
  
  public FileLocation(File file) {
    this(file, 0);
  }
  public FileLocation(File file, int line) {
    this(file, line, 0);
  }
  public FileLocation(File file, int line, int col) {
    this(file, line, col, line, col);
  }
  public FileLocation(File file, int line, int col, int endLine, int endCol) {
    this.file = file;
    this.line = line;
    this.col = col;
    this.endLine = endLine;
    this.endCol = endCol;
  }
  
  public String toString() {
    String result = file.getPath();
    if (line > 0) {
      result += "(" + line;
      if (col > 0) result += "," + col;
      if (endLine != line || endCol != col) {
        result += "-";
        if (endLine != line) result += endLine;
        if (endLine != line && endCol > 0) result += ",";
        if (endCol > 0) result += endCol;
      }
      result += ")";
    }
    return result;
  }
}
