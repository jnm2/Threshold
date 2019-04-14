using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Threshold
{
    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            var assembly = typeof(Program).Assembly;
            Console.WriteLine(assembly.GetProductNameAndVersion() + " – https://github.com/jnm2/Threshold");
            Console.WriteLine(assembly.GetCopyright());
            Console.WriteLine();

            switch (ConsoleUtils.Choose("Would you like to [c]reate a new backup or [r]estore an existing backup?"))
            {
                case 'c':
                    Console.WriteLine();
                    new CreateBackupWizard().Run();
                    break;
                case 'r':
                    throw new NotImplementedException();
            }
        }
    }
}
