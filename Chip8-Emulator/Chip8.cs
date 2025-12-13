namespace Chip8_Emulator;

using System;
using System.IO;
public class Chip8
{
    // ########### HARDWARE ############### //
    
    // -- MEMORY: an array of 4096 bytes -- //
    public byte[] memory = new byte[4096];
    
    // -- GENERAL REGISTERS, a workbench for actually perform operations with the memory bytes -- //
    private byte[] general_registers = new byte[16];
    
    // -- INDEX REGISTER: A pointer for the memory -- //
    private ushort index_register = 0;
    
    //-- PROGRAM COUNTER: A pointer for executing instructions -- //
    private ushort program_counter = 0; 
    
    // -- STACK: An array of return addresses when the CPU calls a subroutine -- //
    private ushort[] stack = new ushort[16];
    private byte stack_pointer = 0;
    
    // -- TIMERS: Delay and Sound timers -- //
    private byte delay_timer = 0;
    private byte sound_timer = 0;
    
    // ########### SOFTWARE ############### //
    private byte[] display = new byte[64 * 32];
    
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
    
    
    // - Takes information from memory and performs operations - //
    public void EmulateCycle()
    {
        // -- FETCH -- //
        // - Takes the next operation to perform (2 bytes) by gluing the "Memory[{PC, PC +1}]" in Binary - // 
        ushort opcode = (ushort)(memory[program_counter] << 8 | memory[program_counter + 1]);
        Console.WriteLine($"PC: {program_counter} | Opcode: {opcode}");
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
                            int target = (byte)((targetY * 64) + targetX);
                            
                            // - If collision (1 XOR 1), Check the flag. Paint the pixel - //
                            if (display[target] == 1) general_registers[0xF] = 1;
                            display[target] ^= 1;
                        }
                    }
                }

                break;
                
        }
    }
    
    // -- Loads a file (in binary) and puts it's info into the memory -- //
    public void LoadRom(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // -- Memory already has 512 bytes reserved for interpreter + font  (4096 - 512 = 3584)-- //
            if (fs.Length > 3584) Console.WriteLine("[SYSTEM] ROM is too large"); return;
            // -- (Buffer, Offset in Buffer, Count) -- //
            fs.Read(memory, 512, (int)fs.Length);
        }
    }
    
    // -- CONSTRUCTOR, like a turn on button -- //
    public Chip8()
    {
        program_counter = 512; // -- The first 512 bits are reserved for static data --
        general_registers = new byte[16];
        delay_timer = 0;
        sound_timer = 0;
        Array.Copy(fontSet, 0, memory, 0, fontSet.Length);
    }

}