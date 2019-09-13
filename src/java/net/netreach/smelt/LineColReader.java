///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.smelt.LineColReader
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/18
// Description: A trivial wrapper around gnu.text.LineBufferedReader.
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

package net.netreach.smelt;

// I/O classes used.
import gnu.text.LineBufferedReader;

import java.io.Reader;

/**
 * A trivial wrapper around gnu.text.LineBufferedReader. This class is used to
 * keep the relationship between nrdo and LineBufferedReader at "arm's length",
 * due to the potential license issues. If the license terms for
 * LineBufferedReader are unacceptable to you, just make a few (commented)
 * changes to this file to remove all dependency on it. Doing so, of course,
 * will also lose the extra functionality it provides: specifically, the ability
 * to know which columns, as well as lines, errors occur on. The lines to change
 * are commented with the string LBRDEP:.
 */
public class LineColReader extends LineBufferedReader /* LineNumberReader */{
  // LBRDEP: Switch the commenting in the above line.

  public LineColReader(Reader in) {
    super(in);

    // LBRDEP: Comment out the following.
    // setConvertCR(true);
  }

  // LBRDEP: Uncomment the following method.
  // public int getColumnNumber() {
  // return -1;
  // }
}
