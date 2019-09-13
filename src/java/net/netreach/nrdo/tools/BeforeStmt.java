///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.BeforeStmt
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2003/05/06
// Description: Represents a 'before' statement.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2001 NetReach, Inc.
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// The GNU General Public License should be located in the file COPYING.gpl.
// For more information on the specific license terms for nrdo, please see the
// file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.nrdo.tools;

// Collections classes used to implement the appropriate behavior.
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

import net.netreach.util.Mappable;

/**
 * Represents a 'before' statement.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class BeforeStmt implements Mappable {

  public String step;
  public String name;
  public String sql;
  public boolean initially = true;
  public boolean upgrade = true;

  private Map asMap;

  public Map toMap() {
    if (asMap == null) {
      asMap = new HashMap();
      asMap.put("step", step);
      asMap.put("name", name);
      asMap.put("statement", sql);
      asMap.put("initial", new Boolean(initially));
      asMap.put("upgrade", new Boolean(upgrade));
    }
    return asMap;
  }

  public static final Set legalBefores = new HashSet(Arrays
      .asList(new String[] { "pre-upgrade-hooks", "dropping-storedprocs", "dropping-seqs",
          "dropping-fkeys", "dropping-indexes", "altering-fields",
          "dropping-changed-fields", "setting-null", "dropping-tables",
          "renaming-tables", "adding-tables", "adding-fields",
          "setting-notnull", "adding-indexes", "adding-fkeys", "adding-seqs",
          "adding-storedprocs", "dropping-fields", "finishing" }));
}
