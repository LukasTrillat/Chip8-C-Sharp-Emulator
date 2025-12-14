using System;
using System.IO;

namespace Chip8_Emulator;

class  Program
{
    static void CreateTestRom()
    {
        byte[] rom = new byte[]
        {
            // --- THE PROGRAM (Starts at memory 512) ---
            0x00, 0xE0, // 00E0: Clear Screen
            0x60, 0x05, // 6005: Set Register V0 to 5 (X coordinate)
            0x61, 0x05, // 6105: Set Register V1 to 5 (Y coordinate)
            0xA2, 0x0C, // A20C: Set Index Register I to 524 (Address of the sprite below)
            0xD0, 0x17, // D015: Draw Sprite at (V0, V1) with height 7
            0x12, 0x0A, // 1200: Jump to 512 (Infinite loop, so it doesn't crash)

            // --- THE DATA (Stored at memory 524) ---
            // A simple 8x5 Sprite (A smiley face)
            0b00111100, // ..####..
            0b01000010, // .#....#.
            0b10100101, // #.#..#.#  (Eyes)
            0b10000001, // #......#
            0b10111101, // #.####.#  (Mouth)
            0b01000010, // .#....#.
            0b00111100  // ..####..
        };

        File.WriteAllBytes("test_rom.ch8", rom);
        Console.WriteLine("[SETUP] Test ROM created.");
    }
    static void Main()
    {
        CreateTestRom();

        Chip8 cpu = new Chip8();

        cpu.LoadRom("test_rom.ch8");

        cpu.Run();
 
    }
}