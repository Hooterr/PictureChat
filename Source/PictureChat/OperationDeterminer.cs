using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PictureChat
{
    public static class OperationDeterminer
    {
        private static readonly string[] DecryptPrefixes = { "-decrypt", "--decrypt", "-d", "--d" };
        private static readonly string[] EncryptPrefixes = { "-encrypt", "--encrypt", "-e", "--e" };

        public static Operation GetOperation(string prefix)
        {
            if (DecryptPrefixes.Contains(prefix))
                return Operation.Decrypt;
            else if (EncryptPrefixes.Contains(prefix))
                return Operation.Encrypt;
            return Operation.Unknown;
        }

    }
}
