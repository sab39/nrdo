///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.cgl.SepWriter
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/05/31
// Description: Part of CGL interpreter
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

// Collections classes used in implementation.
import java.io.IOException;
import java.io.Writer;

class SepWriter extends Writer {
  Entry seps;
  Writer out;

  private class Entry {
    Entry next;
    String sep;
    boolean doit = false;
    boolean maybe = false;

    Entry(Entry next, String sep) {
      this.next = next;
      this.sep = sep;
    }
  }

  SepWriter(Writer out) {
    this.out = out;
    this.seps = new Entry(null, null);
  }

  void startSep(String sep) {
    seps = new Entry(seps, sep);
  }

  void nextSep() {
    seps.doit = true;
  }

  void endSep() {
    seps = seps.next;
  }

  private void sep() throws IOException {
    Entry s = seps;
    while (s.sep != null) {
      if (s.doit && s.maybe) {
        out.write(s.sep);
        s.doit = false;
        break;
      }
      s.doit = false;
      s.maybe = true;
      s = s.next;
    }
  }

  public void write(int i) throws IOException {
    sep();
    out.write(i);
  }

  public void write(char[] c) throws IOException {
    sep();
    out.write(c);
  }

  public void write(char[] c, int from, int to) throws IOException {
    sep();
    out.write(c, from, to);
  }

  public void write(String s) throws IOException {
    sep();
    out.write(s);
  }

  public void write(String s, int from, int to) throws IOException {
    sep();
    out.write(s, from, to);
  }

  public void flush() throws IOException {
    out.flush();
  }

  public void close() throws IOException {
    out.close();
  }
}
