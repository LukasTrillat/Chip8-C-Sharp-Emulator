using System;
using System.IO;
using Raylib_cs;

namespace Chip8_Emulator;

class  Program
{
    static void Main()
    {
        // -- Setup for the window -- //
        const int screenWidth = 640;
        const int screenHeight = 320;
        const int scale = 10;
        
        Raylib.InitWindow(screenWidth,screenHeight, "Chip8 Emulator By Macalusso");
        Raylib.SetTargetFPS(60);
        
        // -- Initialize the CPU -- //
        Chip8 cpu = new Chip8();
        cpu.LoadRom("ROMS/space_invaders.ch8");
        
        // -- Create Video Texture -- //
        Image screenImage = Raylib.GenImageColor(64, 32, Color.Black);
        Texture2D screenTexture = Raylib.LoadTextureFromImage(screenImage);
        
        // -- Prepare Input Handling -- //
        void ProcessInputs()
        {  
            // - Reset the keypads - //
            Array.Clear(cpu.keypad, 0, cpu.keypad.Length);
            
            // - Check inputs - //
            if (Raylib.IsKeyDown(KeyboardKey.One))cpu.keypad[0x1] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.Two))cpu.keypad[0x2] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.Three))cpu.keypad[0x3] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.Four))cpu.keypad[0xC] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.Q))cpu.keypad[0x4] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.W))cpu.keypad[0x5] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.E))cpu.keypad[0x6] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.R))cpu.keypad[0xD] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.A))cpu.keypad[0x7] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.S))cpu.keypad[0x8] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.D))cpu.keypad[0x9] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.F))cpu.keypad[0xE] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.Z))cpu.keypad[0xA] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.X))cpu.keypad[0x0] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.C))cpu.keypad[0xB] = 1;
            if (Raylib.IsKeyDown(KeyboardKey.V))cpu.keypad[0xF] = 1;
        }
        
        // -- Game Loop -- //
        var currentColor = Color.Black; 
        byte[] prevFrame = new byte[64 * 32];
        while (!Raylib.WindowShouldClose())
        {
            // -- Emulating a cpu cycle ten times per frame -- //
            for (byte i=0; i < 15; i ++) cpu.EmulateCycle();
            cpu.UpdateTimers();
            
            // -- Check inputs -- //
            ProcessInputs();
            
            Raylib.BeginDrawing();
            //Raylib.ClearBackground(Color.Black);
            
            for (int i = 0; i < 2048; i++)
            {
                if (cpu.display[i] == 1 || prevFrame[i] == 1)currentColor = Color.White;
                else currentColor = Color.Black;
                
                Raylib.ImageDrawPixel(ref screenImage, i % 64,i / 64, currentColor);
            }
            
            unsafe { Raylib.UpdateTexture(screenTexture, screenImage.Data); }

            Rectangle source = new Rectangle(0, 0, 64, 32);
            Rectangle dest = new Rectangle(0,0, screenWidth, screenHeight);
            // Args: Texture, Source, Dest, Origin (Pivot), Rotation, Tint
            Raylib.DrawTexturePro(screenTexture, source,dest, System.Numerics.Vector2.Zero, 0f, Color.White);
            
            Raylib.EndDrawing();
            Array.Copy(cpu.display, prevFrame, 2048);
            
        }
        Raylib.CloseWindow();
    }
}