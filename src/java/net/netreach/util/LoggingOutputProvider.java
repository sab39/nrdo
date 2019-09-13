package net.netreach.util;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.Calendar;
import java.util.Date;

public class LoggingOutputProvider implements OutputProvider {
  public File logFile;

  public LoggingOutputProvider(File logFile) {
    this.logFile = logFile;
  }

  public void println(String str) {
    log(str);
  }

  public void reportError(FileLocation loc, String message) {
    if (loc != null) {
      log("E: " + loc + ": " + message);
    } else {
      log("E: " + message);
    }
  }

  private void log(String str) {
    if (logFile == null) return;
    try {
      FileWriter out = new FileWriter(logFile, true);
      try {
        PrintWriter pout = new PrintWriter(out);
        try {
          pout.println(time() + ": " + str);
        } finally {
          pout.close();
        }
      } finally {
        out.close();
      }
    } catch (IOException e) {
      // Can't do much with this - trying to write to Output will give an
      // infinite recursion.
    }
  }

  private static String time() {
    Calendar cal = Calendar.getInstance();
    cal.setTime(new Date());
    int y = cal.get(Calendar.YEAR);
    int M = cal.get(Calendar.MONTH);
    int d = cal.get(Calendar.DAY_OF_MONTH);
    int h = cal.get(Calendar.HOUR_OF_DAY);
    int m = cal.get(Calendar.MINUTE);
    int s = cal.get(Calendar.SECOND);
    return y + "/" + dig2(M) + "/" + dig2(d) + " " + dig2(h) + ":" + dig2(m)
        + ":" + dig2(s);
  }

  private static String dig2(int i) {
    return (i < 10 ? "0" : "") + i;
  }

  public void setCurrentProgress(int currentProgress) {
  }

  public void setTotalProgress(int totalProgress) {
  }
}