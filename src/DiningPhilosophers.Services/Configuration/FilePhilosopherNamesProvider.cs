using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Configuration;

namespace DiningPhilosophers.Services.Configuration
{
    public class FilePhilosopherNamesProvider : IPhilosopherNamesProvider
    {
        private readonly string _file;

        public FilePhilosopherNamesProvider(string file)
        {
            _file = file;
        }

        public IEnumerable<string> GetNames()
        {
            if (!File.Exists(_file))
                throw new FileNotFoundException($"Файл {_file} не найден.");

            var names = File.ReadAllLines(_file)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .ToArray();

            if (names.Length != 5)
                throw new Exception("Требуется ровно 5 философов.");

            return names;
        }
    }
}
