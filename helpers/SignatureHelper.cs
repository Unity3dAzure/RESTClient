// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

#if NETFX_CORE
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace RESTClient {
  public static class SignatureHelper {
    public static string Sign(byte[] key, string stringToSign) {
#if NETFX_CORE
			MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
			CryptographicHash hash = provider.CreateHash(key.AsBuffer());
			hash.Append(CryptographicBuffer.ConvertStringToBinary(stringToSign, BinaryStringEncoding.Utf8));
			return CryptographicBuffer.EncodeToBase64String( hash.GetValueAndReset() );
#else
      var hmac = new HMACSHA256();
      hmac.Key = key;
      byte[] sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
      return Convert.ToBase64String(sig);
#endif
    }
  }
}
