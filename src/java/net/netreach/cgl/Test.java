///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.Test
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/10
// Description: Unit testing for CGL.
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

package net.netreach.cgl;

// Collections classes used in the implementation.
import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

import net.netreach.util.FileLocatedException;
import net.netreach.util.FileLocation;
import net.netreach.util.Output;

public class Test {
  public static void main(String[] args) throws IOException, FileLocatedException {
    File testDir = new File("tests/tests");
    File resultDir = new File("tests/expected");
    File failDir = new File("tests/failures");
    File[] testFiles = testDir.listFiles();
    java.util.Arrays.sort(testFiles);
    int evalPassCount = 0;
    int writePassCount = 0;
    int testCount = 0;
    int namelen = 0;
    for (int i = 0; i < testFiles.length; i++) {
      if (testFiles[i].getName().length() > namelen) {
        namelen = testFiles[i].getName().length();
      }
    }
    for (int i = 0; i < testFiles.length; i++) {
      if (!testFiles[i].isFile()) {
        continue;
      }
      String expected = readFile(new File(resultDir, testFiles[i].getName()
          + ".out"));
      if (expected == null) {
        Output.reportError(new FileLocation(testFiles[i]), "No .out file found");
      } else {
        testCount++;
        Expr exp = CGLParser.loadTemplate(testFiles[i]);
        System.out.print(testFiles[i].getName() + ": ");
        for (int j = testFiles[i].getName().length(); j < namelen; j++) {
          System.out.print(" ");
        }
        System.out.flush();
        String result = exp.evaluate(makeContext()).toString();
        if (expected.equals(result)) {
          System.out.print("PASS(eval), ");
          evalPassCount++;
        } else {
          System.out.print("FAIL(eval), ");
          writeFile(new File(failDir, testFiles[i].getName() + ".out"), result);
        }
        System.out.flush();
        StringBuffer sb = new StringBuffer();
        StringBufferWriter out = new StringBufferWriter(sb);
        exp.write(makeContext(), out);
        String writeResult = sb.toString();
        if (expected.equals(writeResult)) {
          Output.println("PASS(write).");
          writePassCount++;
        } else {
          Output.println("FAIL(write).");
          if (!writeResult.equals(result)) {
            writeFile(new File(failDir, testFiles[i].getName() + ".out"
                + (expected.equals(result) ? "" : ".wr")), writeResult);
          }
        }
        System.out.flush();
      }
    }
    Output.println("Passed " + evalPassCount + "/" + testCount + "(eval), "
        + writePassCount + "/" + testCount + "(write)");
  }

  static String readFile(File file) throws IOException {
    if (!file.exists()) return null;
    Reader in = new BufferedReader(new FileReader(file));
    StringBuffer sb = new StringBuffer();
    int ch = 0;
    while ((ch = in.read()) >= 0) {
      sb.append((char) ch);
    }
    in.close();
    return sb.toString();
  }

  static void writeFile(File file, String str) throws IOException {
    Writer out = new FileWriter(file);
    out.write(str);
    out.close();
  }

  static Context makeContext() {
    Context ctx = new Context();
    ctx.vars.put("one", new Integer(1));
    ctx.vars.put("two", new Integer(2));
    ctx.vars.put("three", new Integer(3));
    ctx.vars.put("hello", "Hello World");
    ctx.vars.put("spoon", "There is no spoon");
    List people = new ArrayList();
    HashMap person = new HashMap();
    person.put("name", "Crowley");
    person.put("occupation", "Demon");
    person.put("transport", "Bentley");
    people.add(person);
    person = new HashMap();
    person.put("name", "Aziraphale");
    person.put("occupation", "Angel");
    person.put("transport", null);
    people.add(person);
    person = new HashMap();
    person.put("name", "Madame Tracy");
    person
        .put(
            "occupation",
            "Painted Jezebel (Tuesdays and Fridays only, Thursdays by arrangement) and Medium");
    person.put("transport", "scooter");
    people.add(person);
    person = new HashMap();
    person.put("name", "Newton Pulsifer");
    person.put("occupation", "Witchfinder (part-time)");
    person.put("transport", "Wasabi");
    people.add(person);
    person = new HashMap();
    person.put("name", "Anathema Device");
    person.put("occupation", "Occultist (part-time), Witch, Descendent");
    person.put("transport", "bike");
    people.add(person);
    ctx.vars.put("people", people);
    return ctx;
  }

  static class StringBufferWriter extends Writer {
    StringBuffer sb;

    StringBufferWriter(StringBuffer sb) {
      this.sb = sb;
    }

    public void write(int i) {
      sb.append(i);
    }

    public void write(char[] c) {
      sb.append(new String(c));
    }

    public void write(char[] c, int from, int to) {
      sb.append(new String(c).substring(from, to));
    }

    public void write(String s) {
      sb.append(s);
    }

    public void write(String s, int from, int to) {
      sb.append(s.substring(from, to));
    }

    public void flush() {
    }

    public void close() {
    }
  }
}
