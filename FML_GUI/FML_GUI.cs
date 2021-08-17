using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using RaylibSharp;
using FishMarkupLanguage;
using System.Runtime.InteropServices;

namespace FML_GUI {
	unsafe class Program {
		static FMLDocument Doc;

		static void Main(string[] args) {
			Doc = FML.Parse("test_gui.fml");
			Doc.FlattenTemplates();

			TempText = (char*)Marshal.AllocHGlobal(512);
			TempText[0] = (char)0;

			StartGraphics();
		}

		static int ParentX;
		static int ParentY;
		static char* TempText;

		static void DrawFMLTag(FMLTag Tag) {
			int X = Tag.Attributes.GetAttribute<int>("X", 0);
			int Y = Tag.Attributes.GetAttribute<int>("Y", 0);
			int W = Tag.Attributes.GetAttribute<int>("W", 0);
			int H = Tag.Attributes.GetAttribute<int>("H", 0);

			string Title = Tag.Attributes.GetAttribute("Title", "");

			int GlobalX = ParentX + X;
			int GlobalY = ParentY + Y;


			if (Tag.TagName == "Window") {
				//Raylib.DrawRectangle(X, Y, W, H, new Color(66, 66, 66, 100));
				//Raylib.DrawRectangleLines(X, Y, W, H, new Color(0, 0, 0));

				Raygui.GuiWindowBox(new Rectangle(GlobalX, GlobalY, W, H), Title);
			}

			if (Tag.TagName == "Button") {
				Raygui.GuiButton(new Rectangle(GlobalX, GlobalY, W, H), Title);
			}

			if (Tag.TagName == "Textbox") {
				Raygui.GuiTextBox(new Rectangle(GlobalX, GlobalY, W, H), TempText, 512, true);
			}

			if (Tag.TagName == "Dropdown") {

			}

			foreach (FMLTag C in Tag.Children) {
				ParentX = X;
				ParentY = Y;

				DrawFMLTag(C);
			}
		}

		static void StartGraphics() {
			FMLTag MainWindow = Doc.Tags[0];

			int X = MainWindow.Attributes.GetAttribute("X", 100);
			int Y = MainWindow.Attributes.GetAttribute("Y", 100);

			int Width = MainWindow.Attributes.GetAttribute("W", 800);
			int Height = MainWindow.Attributes.GetAttribute("H", 600);

			string Title = MainWindow.Attributes.GetAttribute("Title", "");

			Raylib.SetConfigFlags((uint)ConfigFlag.FLAG_WINDOW_UNDECORATED);
			Raylib.InitWindow(Width, Height, Title);
			Raylib.SetTargetFPS(60);

			Vector2 mousePosition = new Vector2(0, 0);
			Vector2 windowPosition = new Vector2(X, Y);
			Vector2 panOffset = mousePosition;
			bool dragWindow = false;
			bool shouldClose = false;

			Raylib.SetWindowPosition((int)windowPosition.X, (int)windowPosition.Y);

			while (!shouldClose && !Raylib.WindowShouldClose()) {
				// Update
				//----------------------------------------------------------------------------------
				mousePosition = Raylib.GetMousePosition();

				if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON)) {
					if (Raylib.CheckCollisionPointRec(mousePosition, new Rectangle(0, 0, Width, 20))) {
						dragWindow = true;
						panOffset = mousePosition;
					}
				}

				if (dragWindow) {
					windowPosition.X += (mousePosition.X - panOffset.X);
					windowPosition.Y += (mousePosition.Y - panOffset.Y);

					if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON))
						dragWindow = false;

					Raylib.SetWindowPosition((int)windowPosition.X, (int)windowPosition.Y);
				}

				// Drawing

				Raylib.BeginDrawing();
				Raylib.ClearBackground(new Color(125, 167, 184));

				if (Raygui.GuiWindowBox(new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()), "FML GUI")) {
					shouldClose = true;
				}

				if (!shouldClose) {
					foreach (FMLTag T in MainWindow.Children) {
						ParentX = 0;
						ParentY = 0;

						DrawFMLTag(T);
					}
				}

				Raylib.EndDrawing();

				if (shouldClose)
					Raylib.CloseWindow();
			}
		}
	}
}
