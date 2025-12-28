using System.Threading;

namespace Chip8_Emulator;

using System;
using System.IO;
using System.Text;
public class Chip8
{
    // ########### HARDWARE ############### //
    
    // -- MEMORY: an array of 4096 bytes -- //
    public byte[] memory = new byte[4096];
    
    // -- GENERAL REGISTERS, a workbench for actually perform operations with the memory bytes -- //
    private byte[] general_registers = new byte[16];
    
    // -- INDEX REGISTER: A pointer for the memory -- //
    private ushort index_register;
    
    //-- PROGRAM COUNTER: A pointer for executing instructions -- //
    private ushort program_counter; 
    
    // -- STACK: An array of return addresses when the CPU calls a subroutine -- //
    private ushort[] stack = new ushort[16];
    private byte stack_pointer;
    
    // -- TIMERS: Delay and Sound timers -- //
    private byte delay_timer;
    private byte sound_timer;
    
    // -- KEYPAD -- //
    public byte[] keypad = new byte[16];
    private int[] keyTimers = new int[16];
    
    
    // ########### SOFTWARE ############### //
    
    // -- Display "screen" -- //
    public byte[] display = new byte[64 * 32];
    
    // -- FontSet of hexadecimal characters -- //
    private readonly byte[] fontSet = 
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };
    
    // -- RNG --
    private Random rng = new Random();
    
    
    
    // - FETCH - DECODE - EXCECUTE - //
    public void EmulateCycle()
    {
        // -- FETCH -- //
        // - Takes the next operation to perform (2 bytes) by gluing the "Memory[{PC, PC +1}]" in Binary - // 
        ushort opcode = (ushort)(memory[program_counter] << 8 | memory[program_counter + 1]);
        program_counter += 2;
    
        // -- DECODE -- //
        // - "What to do" - //
        byte instruction = (byte)((opcode & 0xF000) >> 12);
        // - "Where to do it" - //
        byte x = (byte)((opcode & 0x0F00) >> 8);
        byte y = (byte)((opcode & 0x00F0) >> 4);
        // - "Which value to use" - //
        ushort nnn = (ushort)(opcode & 0x0FFF);
        byte nn = (byte)(opcode & 0x00FF);
        
        // -- EXCECUTE -- //
        switch (instruction)
        {
            // -  Return || Clear Screen - //
            case 0x0:
                //- Return - //
                if (nn == 0xEE)
                {
                    stack_pointer--;
                    program_counter = stack[stack_pointer];
                }
                // - Clear - //
                else {Array.Clear(display, 0, display.Length);}
                break;
            
            // - Jump - //
            case 0x1:
                program_counter = nnn;
                break;
            
            // - [Subroutine] Call instruction: Perform a task, but save the previous PC - //
            case 0x2:
                stack[stack_pointer] = program_counter;
                stack_pointer++;
                program_counter = nnn;
                break;
            
            // - Set Register - //
            case 0x6:
                general_registers[x] = nn;
                break;
            
            // - Add to Register - //
            case 0x7:
                general_registers[x] += nn;
                break;
            
            // - Set Index Register - //
            case 0xA:
                index_register = nnn;
                break;
            
            // - Draw instruction - //
            case 0xD:
                
                // - Gets the coordenates where it's going to draw - //
                byte coordX = general_registers[x];
                byte coordY = general_registers[y];
                
                // - The height (rows 0-15) - //
                byte rows = (byte)(opcode & 0x000F);
                
                // - Sets the collision flag to False - //
                general_registers[0xF] = 0;
                
                // - Scan every row - //
                for (byte row = 0; row < rows; row++)
                {
                    // - Get the pixel information (which column is 1 [ON] or 0 [OFF])
                    byte row_pixel_info = memory[index_register + row];
                    // - Scanns every bit in that row's byte - //
                    for (byte column = 0; column < 8; column++)
                    {
                        // - If it's 1 [ON] - //
                        if (((row_pixel_info >> (7 - column)) & 1) == 1)
                        {
                            // - Gets the target coordinate to draw (overflows at 64x32) - //
                            byte targetX = (byte)((coordX + column) % 64);
                            byte targetY = (byte)((coordY + row) % 32);
                            
                            // - Display is a 1D Array - //
                            int target = ((targetY * 64) + targetX);
                            
                            // - If collision (1 XOR 1), Check the flag. Paint the pixel - //
                            if (display[target] == 1) general_registers[0xF] = 1;
                            display[target] ^= 1;
                        }
                    }
                }

                break;
            
            // - Skip instructions - //
            case 0xE:
                // - Depending on the value of nn, check if a KEY (which index is stored in VX) is being pressed or not- // 
                if (nn == 0x9E && keypad[general_registers[x]] == 1) program_counter += 2; // - 9E and key pressed? Skip - //
                else if (nn == 0xA1 && keypad[general_registers[x]] == 0) program_counter += 2; // - A1 and key not pressed? Skip - // 
                break;
            
            // - Wait instruction - //
            case 0xF:
                switch (nn)
                {
                    // - GET the value of the DELAY TIMER - // 
                    case 0x07:
                        general_registers[x] = delay_timer;
                        break;
                    
                    // - SET the value of the DELAY TIMER - // 
                    case 0x15:
                        delay_timer = general_registers[x];
                        break;
                    
                    // - SET the value of the SOUND TIMER - // 
                    case 0x18:
                        sound_timer = general_registers[x];
                        break;
                    
                    // - Wait for an input - //
                    case 0x0A:
                        bool keyPressed = false;
                        
                        for (byte i = 0; i < keypad.Length; i++)
                        {
                            if (keypad[i] == 1)
                            {
                                general_registers[x] = i;
                                keyPressed = true;
                                break;
                            }
                        }
                        // - Don't continue until a key is pressed - //
                        if (!keyPressed) program_counter -= 2;
                        break;
                    
                    // - Add to Index - //
                    case 0x1E:
                        index_register += general_registers[x];
                        break;
                    
                    // - Font Helper - //
                    case 0x29:
                        index_register = (ushort)(general_registers[x] * 5); // - Font is loaded at V0, and characters are 5 bytes long - // 
                        break;
                    
                    // - ScoreBoard - //
                    case 0x33:
                        memory[index_register] = (byte)(general_registers[x] / 100); // - First Digit - //
                        memory[index_register + 1] = (byte)((general_registers[x] / 10) % 10); // - Second Digit - //
                        memory[index_register + 2] = (byte)(general_registers[x] % 10); // - Third Digit - //
                        break;
                    
                    case 0x55:
                        for (byte i = 0; i<= x; i++)
                        {
                            memory[index_register + 1] = general_registers[i];
                        }
                        index_register += (ushort)(x + 1); 
                        break;
                    case 0x65:
                        for (byte i = 0; i <= x; i++)
                        {
                            general_registers[i] = memory[index_register + 1];
                        }
                        index_register += (ushort)(x + 1); 
                        break;
                }
                break;
            
            // - Math operators - //
            case 0x8:
                byte n = (byte)(opcode & 0x000F);
                switch (n)
                {
                    // - SET: Vx = Vy - //
                    case 0x0:
                        general_registers[x] = general_registers[y];
                        break;
                    
                    // - OR: Vx = Vx OR Vy - //
                    case 0x1:
                        general_registers[x] = (byte)(general_registers[x] | general_registers[y]);
                        break;
                    
                    // - AND: Vx = Vx AND Vy - //
                    case 0x2:
                        general_registers[x] = (byte)(general_registers[x] & general_registers[y]);
                        break;
                    
                    // - XOR: Vx = Vx XOR Vy - //
                    case 0x3:
                        general_registers[x] = (byte)(general_registers[x] ^ general_registers[y]);
                        break;
                    
                    // - ADD: Vx = Vx + Vy
                    case 0x4:
                        int add_result = (general_registers[x] + general_registers[y]);
                        general_registers[0xF] = (add_result > 255) ? (byte)1 : (byte)0; // - Checks overflow: VF = 1 if true, = 0 if not.
                        general_registers[x] = (byte)add_result;
                        break;
                    
                    // - SUBSTRACT: Vx = Vx - Vy - //
                    case 0x5:
                        int sus_result = (general_registers[x] - general_registers[y]);
                        general_registers[0xF] = (general_registers[x] >= general_registers[y]) ? (byte)1 : (byte)0; // - Checks positive: VF = 1 if true, = 0 if not.
                        general_registers[x] = (byte)sus_result;
                        break;
                    
                    // - REVERSE SUBSTRACT: Vx = Vy - Vx - //
                    case 0x7:
                        int inv_sus_result = (general_registers[y] - general_registers[x]);
                        general_registers[0xF] = (general_registers[y] >= general_registers[x]) ? (byte)1 : (byte)0; // - Checks positive: VF = 1 if true, = 0 if not.
                        general_registers[x] = (byte)inv_sus_result;
                        break;
                    
                    // - SHIFT RIGHT: Vx = Vx >> 1 - //
                    case 0x6:
                        general_registers[0xF] = (byte)(general_registers[x] & 0x1); // - Catches the lost (rightmost) bit
                        general_registers[x] >>= 1;
                        break;
                    
                    // - SHIFT LEFT: Vx = Vx << 1  - //
                    case 0xE:
                        general_registers[0xF] = (byte)((general_registers[x] >> 7) & 0x1); // - Catches the lost (leftmost) bit
                        general_registers[x] <<= 1;
                        break;
                }
                break;
            
            // - Randomness (RNG) - //
            case 0xC:
                general_registers[x] = (byte)((byte)rng.Next(0, 256) & (nn));
                
                break;
            
            // - Conditional Skips - //
            case 0x3:
                if (general_registers[x] == nn) program_counter += 2;
                break;
            case 0x4:
                if (general_registers[x] != nn) program_counter += 2;
                break;
            case 0x5:
                if(general_registers[x] == general_registers[y]) program_counter += 2;
                break;
            case 0x9:
                if(general_registers[x] != general_registers[y]) program_counter += 2;
                break;
            
            
            default:
                Console.WriteLine($"[SYSTEM] Unknown Opcode: {opcode:X4}");
                // Optional: Pause so you can read the error
                Console.ReadLine(); 
                break;
                
        }
    }
    
    // -- Loads a file (in binary) and puts it's info into the memory -- //
    public void LoadRom(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // -- Memory already has 512 bytes reserved for interpreter + font  (4096 - 512 = 3584)-- //
            if (fs.Length > 3584) {Console.WriteLine("[SYSTEM] ROM is too large"); return;}
            // -- (Buffer, Offset in Buffer, Count) -- //
            fs.Read(memory, 512, (int)fs.Length);
        }
    }
    
    // -- Updates timers -- //
    public void UpdateTimers()
    {
        if (sound_timer > 0)
        {
            sound_timer --; 
            Console.WriteLine("[SYSTEM] Beep!!");
        }
        if (delay_timer > 0) delay_timer--;

    }
    
    // -- Draws the "Display" array to the console -- //
        // ---- CONSOLE VERSION ONLY ---- //
    /*public void DrawToConsole()
    {
        Console.SetCursorPosition(0, 0);
        StringBuilder console = new StringBuilder();
        
        for (byte row = 0; row < 32; row++)
        {
            for (byte column = 0; column < 64; column++)
            {
                if (display[row * 64 + column] == 1) console.Append("█");
                else console.Append(" ");
            }

            console.Append('\n');
        }

        Console.Write(console.ToString());
    }*/
    
    // -- Run Loop -- //
    public void Run()
    {
        while (true)
        {
            // ProcessInputs(); -- For Console Version -- //
            for (byte i = 0; i < 8; i ++){ EmulateCycle();}
            UpdateTimers();
            // DrawToConsole(); -- For Console version -- //
            Thread.Sleep(16);

        }


    }
    
    // -- Process Inputs -- //
        // ---- CONSOLE VERSION ONLY ---- //
    /*public void ProcessInputs()
    {
        for (int i = 0; i < 16; i++)
        {
            if (keyTimers[i] > 0)
            {
                keyTimers[i]--; 
                keypad[i] = 1; // Keep the key "Pressed" in the emulator
            }
            else
            {
                keypad[i] = 0; // Time is up, release the key
            }
        }
        
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            int chip8Key = -1;
            
            switch (key)
            {
                case ConsoleKey.D1: chip8Key = 0x1; break;
                case ConsoleKey.D2: chip8Key = 0x2; break;
                case ConsoleKey.D3: chip8Key = 0x3; break;
                case ConsoleKey.D4: chip8Key = 0xC; break;
            
                case ConsoleKey.Q:  chip8Key = 0x4; break; 
                case ConsoleKey.W:  chip8Key = 0x5; break;
                case ConsoleKey.E:  chip8Key = 0x6; break;
                case ConsoleKey.R:  chip8Key = 0xD; break;
            
                case ConsoleKey.A:  chip8Key = 0x7; break;
                case ConsoleKey.S:  chip8Key = 0x8; break;
                case ConsoleKey.D:  chip8Key = 0x9; break;
                case ConsoleKey.F:  chip8Key = 0xE; break;
            
                case ConsoleKey.Z:  chip8Key = 0xA; break;
                case ConsoleKey.X:  chip8Key = 0x0; break;
                case ConsoleKey.C:  chip8Key = 0xB; break;
                case ConsoleKey.V:  chip8Key = 0xF; break;
            }
            
            if (chip8Key != -1)
            {
                keyTimers[chip8Key] = 5; 
            }
        }
    } */
    
    // -- CONSTRUCTOR, like a turn on button -- //
    public Chip8()
    {
        program_counter = 512; // - The first 512 bits are reserved for static data -
        Array.Clear(general_registers); // - Clears all registers -
        // - Resets timers - //
        delay_timer = 0; 
        sound_timer = 0;
        Array.Copy(fontSet, 0, memory, 0, fontSet.Length); // - Puts the fontSet in memory -
    }

}