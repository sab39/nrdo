package net.netreach.util;

import java.io.File;
import java.io.IOException;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;
import java.util.Set;

public class FileCache<T> {
  private Map<File, T> map = new HashMap<File, T>();
  private Map<File, T> old = new HashMap<File, T>();
  private Map<File, Long> timestamps = new HashMap<File, Long>();
  private Populator<T> populator;

  public FileCache(Populator<T> populator) {
    this.populator = populator;
  }
  
  public void reset() {
    map.clear();
  }
  
  public Set<File> getFiles() {
    return Collections.unmodifiableSet(map.keySet());
  }
  
  public T get(File file) throws IOException, FileLocatedException {
    if (!file.exists()) return null;
    
    file = file.getCanonicalFile();
    
    if (map.containsKey(file)) {
      return map.get(file);
    } else if (timestamps.containsKey(file)) {
      long oldStamp = timestamps.get(file);
      if (oldStamp == file.lastModified()) {
        T t = old.get(file);
        map.put(file, t);
        return t;
      }
    }
    
    T t = populator.get(file);
    timestamps.put(file, file.lastModified());
    map.put(file, t);
    old.put(file, t);
    return t;
  }
  
  public static interface Populator<T> {
    T get(File file) throws IOException, FileLocatedException;
  }
}
