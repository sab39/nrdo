package net.netreach.util;

public interface OutputProvider {

  void println(String s);

  void reportError(FileLocation loc, String message);

  void setCurrentProgress(int currentProgress);

  void setTotalProgress(int totalProgress);
}