// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace RESTClient {
  /// <summary>
  /// Interface to support table Query with `$inlinecount=allpages`
  /// </summary>
  public interface INestedResults {
  }

  public interface INestedResults<T> {
    // work-around for WSA
    string GetArrayField();
    string GetCountField();

    void SetArray(T[] array);
    void SetCount(uint count);

  }
}
