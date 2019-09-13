package net.netreach.util;

public class FileLocatedException extends Exception {

  private static final long serialVersionUID = -8970745586669163828L;
  private FileLocation loc;
  
  public FileLocation getLoc() {
    return loc;
  }

  public FileLocatedException(FileLocation loc) {
    this.loc = loc;
  }

  public FileLocatedException(FileLocation loc, String message) {
    super(message);
    this.loc = loc;
  }

  public FileLocatedException(FileLocation loc, Throwable cause) {
    super(cause);
    this.loc = loc;
  }

  public FileLocatedException(FileLocation loc, String message, Throwable cause) {
    super(message, cause);
    this.loc = loc;
  }
  
  public String toString() {
    return (loc == null ? "" : loc.toString()) + super.toString();
  }
}
