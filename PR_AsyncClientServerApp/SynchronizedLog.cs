using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PR_AsyncClientServerApp
{
    class SynchronizedLog
    {
        private ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();
        public String Read(String path)
        {
            rwls.EnterReadLock();
            try
            {
                return File.ReadAllText(path);
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }
        public bool Write(String path, String line)
        {
            if (rwls.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    using (StreamWriter file = new StreamWriter(path, true))
                    {
                        file.WriteLine(line);
                    }
                }
                finally
                {
                    rwls.ExitWriteLock();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        ~SynchronizedLog()
        {
            if (rwls != null)
            {
                rwls.Dispose();
            }
        }
    }
}
