using System;
using Default_Database_Creator;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            createDatabase.createDB();

            Console.ReadKey();
        }
    }
}