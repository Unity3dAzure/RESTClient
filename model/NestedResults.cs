// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace RESTClient {
  /// <summary>
  /// Wrap your data model with this object to call the table Query with `$inlinecount=allpages` param.
  /// </summary>
  [Serializable]
  public sealed class NestedResults<T> : INestedResults<T> {
    public uint count;
    public T[] results;

    // WSA work-around
    public void SetArray(T[] array) {
      this.results = array;
    }

    public void SetCount(uint count) {
      this.count = count;
    }

    public string GetArrayField() {
      return "results";
    }

    public string GetCountField() {
      return "count";
    }
  }
}
