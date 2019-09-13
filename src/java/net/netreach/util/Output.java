package net.netreach.util;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

public class Output {
  private static List outputProviders = new ArrayList();
  private static DefaultOutput defaultOutput = new DefaultOutput();
  private static PromptProvider promptProvider = defaultOutput;
  private static int progressCurrent;
  private static int progressTotal;

  public static void reset() {
    outputProviders.clear();
    defaultOutput = new DefaultOutput();
    promptProvider = defaultOutput;
    progressCurrent = 0;
    progressTotal = 0;
  }
  
  public static void setPromptProvider(PromptProvider provider) {
    promptProvider = provider;
  }

  public static void addOutputProvider(OutputProvider provider, boolean suppressDefault) {
    outputProviders.add(provider);
    if (suppressDefault) defaultOutput = null;
  }

  public static void println(String str) {
    for (Iterator i = outputProviders.iterator(); i.hasNext(); ) {
      OutputProvider provider = (OutputProvider)i.next();
      provider.println(str);
    }
    if (defaultOutput != null) defaultOutput.println(str);
  }

  public static void reportError(FileLocation loc, String message) {
    for (Iterator i = outputProviders.iterator(); i.hasNext(); ) {
      OutputProvider provider = (OutputProvider)i.next();
      provider.reportError(loc, message);
    }
    if (defaultOutput != null) defaultOutput.reportError(loc, message);
  }

  public static void reportException(Throwable t) {
    StringWriter w = new StringWriter();
    t.printStackTrace(new PrintWriter(w, true));
    println(w.toString());
    FileLocation loc = null;
    if (t instanceof FileLocatedException) {
      loc = ((FileLocatedException)t).getLoc();
    }
    reportError(loc, t.getMessage());
  }

  public static boolean prompt(String prompt, String question) {
    boolean result = promptProvider.prompt(prompt, question);
    println("PROMPT: " + prompt);
    println(" - " + question);
    println(" -> " + (result ? "Yes" : "No"));
    return result;
  }

  public static void setCurrentProgress(int current) {
    progressCurrent = current;
    for (Iterator i = outputProviders.iterator(); i.hasNext(); ) {
      OutputProvider provider = (OutputProvider)i.next();
      provider.setCurrentProgress(current);
    }
  }

  public static int getCurrentProgress() {
    return progressCurrent;
  }

  public static void setTotalProgress(int total) {
    progressTotal = total;
    for (Iterator i = outputProviders.iterator(); i.hasNext(); ) {
      OutputProvider provider = (OutputProvider)i.next();
      provider.setTotalProgress(total);
    }
  }

  public static int getTotalProgress() {
    return progressTotal;
  }

  public static PromptProvider getPromptProvider() {
    return promptProvider;
  }
}