using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        List<string[]> cards = new List<string[]>(); // Menyimpan representasi kartu
        
        while (true)
        {
            Console.Write("Masukkan nilai kartu (contoh: 4,5) atau 'exit' untuk keluar: ");
            string input = Console.ReadLine();

            if (input.ToLower() == "exit") break; // Keluar dari loop jika ketik "exit"

            string[] values = input.Split(',');
            if (values.Length != 2 || !int.TryParse(values[0], out int first) || !int.TryParse(values[1], out int second))
            {
                Console.WriteLine("Format salah! Masukkan dua angka dipisahkan koma.");
                continue;
            }

            // Cek apakah kartu adalah double (kembar)
            string[] card;
            if (first == second)
            {
                card = new string[]
                {
                    "┌───┐",
                    $"│ {first} │",
                    $"│ {second} │",
                    "└───┘"
                };
            }
            else
            {
                card = new string[]
                {
                    "┌───┬───┐",
                    $"│ {first} │ {second} │",
                    "└───┴───┘"
                };
            }

            cards.Add(card); // Simpan kartu untuk ditampilkan sejajar
            
            // Cetak semua kartu yang telah dimasukkan
            Console.Clear();
            PrintCards(cards);
        }
    }

    static void PrintCards(List<string[]> cards)
    {
        int maxRows = 4; // Maksimum baris tampilan kartu

        for (int i = 0; i < maxRows; i++) // Loop setiap baris
        {
            foreach (var card in cards)
            {
                if (i < card.Length) // Cek apakah baris ini ada pada kartu
                    Console.Write(card[i].PadRight(10)); // Cetak kartu dengan spasi antar kartu
                else
                    Console.Write(" ".PadRight(10)); // Jika tidak ada baris, tambahkan spasi kosong
            }
            Console.WriteLine();
        }
    }
}
