using System;
using System.Collections.Generic;

// TODO: import enum?

namespace terradota {
  public class ConfigData {
    public string description { get; set; }
    public DefaultData @default { get; set; }
  }

  public class DefaultData {
    public string showName { get; set; }
    public int damage { get; set; }
    // TODO: more properties
  }
}
