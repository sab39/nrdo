package net.netreach.util;

import java.io.IOException;

public class DefaultOutput implements PromptProvider {
  public void println(String str) {
    System.out.println(str);
    System.out.flush();
  }

  public void reportError(FileLocation loc, String message) {
    if (loc != null) System.err.print(loc + ": ");
    System.err.println(message);
    System.err.flush();
  }

  public boolean prompt(String prompt, String question) {
    try {
      System.out.print(prompt + "\n" + question + " [yN]");
      System.out.flush();
      int ch = System.in.read();
      boolean ok = ch == 'Y' || ch == 'y';
      while (ch != '\n' && ch > 0)
        ch = System.in.read();
      return ok;
    } catch (IOException e) {
      return false;
    }
  }
}