package net.netreach.util;

import java.io.IOException;
import java.util.Arrays;
import java.util.List;

public class PromptCmdProvider implements PromptProvider {
  private List promptCmd;

  public PromptCmdProvider(List promptCmd) {
    if (promptCmd == null) throw new NullPointerException("null promptCmd!");
    this.promptCmd = promptCmd;
  }

  public boolean prompt(String prompt, String question) {
    try {
      String[] cmd = new String[promptCmd.size() + 2];
      promptCmd.toArray(cmd);
      cmd[cmd.length - 2] = '"' + prompt.replace('\n', ' ') + '"';
      cmd[cmd.length - 1] = '"' + question.replace('\n', ' ') + '"';
      Output.println("Executing prompt cmd: " + Arrays.asList(cmd));
      Process proc = Runtime.getRuntime().exec(cmd);
      return proc.waitFor() == 0;
    } catch (IOException e) {
      return false;
    } catch (InterruptedException e) {
      return false;
    }
  }
}