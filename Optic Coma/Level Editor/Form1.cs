﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Level_Editor
{
    public partial class frmMain : Form
    {
        public Dictionary<string, Image> LoadedImages;
        public Level DefaultLevel;
        public bool ShowGridLines = true;
        public Level CurrentLevel;
        public LayerMode CurrentLayer = LayerMode.Midground;
        public Tool CurrentTool = Tool.Pan;
        public Point PanOffset;
        BackgroundWorker bW;

        public frmMain()
        {
            InitializeComponent();
        }
        private TileGrid[] LoadAsync(ref BackgroundWorker w)
        {
            List<Tile>
                f = new List<Tile>(),
                m = new List<Tile>(),
                b = new List<Tile>();

            for (int j = 0; j < 40; j++)
            {
                for (int i = 0; i < 40; i++)
                {
                    Tile p = new Tile(new Point(i * 32, j * 32), Properties.Resources.defaultTileImage);

                    m.Add(p);
                    f.Add(p);
                    b.Add(p);
                }
                w.ReportProgress((j * 2));
            }
            TileGrid[] r = { new TileGrid(f), new TileGrid(m), new TileGrid(b) };
            return r;
        }
        private void bW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            for (int i = 1; i <= 10; i++)
            {
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    TileGrid[] a = LoadAsync(ref worker);
                    DefaultLevel = new Level(new Size(320 * 4, 320 * 4), 0, a[0], a[1], a[2]);

                }
            }
        }
        private void bW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            levelLoadProgress.Value = levelLoadProgress.Maximum * (e.ProgressPercentage / 100);
        }
        private void bW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CurrentLevel = DefaultLevel;
            levelLoadProgress.Value = 0;
            CurrentLevel.Display(ref tilePanel, LayerMode.Midground, ShowGridLines, PanOffset);
            tilePanel.Update();
            hScrollBarLevel.Maximum = (CurrentLevel.TileGridDimensions.Width) - tilePanel.Width;
            vScrollBarLevel.Maximum = (CurrentLevel.TileGridDimensions.Height) - tilePanel.Height;
            hScrollBarLevel.Enabled = true;
            vScrollBarLevel.Enabled = true;
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            ActiveForm.StartPosition = FormStartPosition.CenterScreen;

            bW = new BackgroundWorker();
            bW.WorkerReportsProgress = true;
            bW.WorkerSupportsCancellation = true;
            bW.DoWork += new DoWorkEventHandler(bW_DoWork);
            bW.ProgressChanged += new ProgressChangedEventHandler(bW_ProgressChanged);
            bW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bW_RunWorkerCompleted);
            bW.RunWorkerAsync();
        }
        private void frmMain_Resize(object sender, EventArgs e)
        {

        }

        private void showGridlinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowGridLines)
            {
                ShowGridLines = false;
                CurrentLevel.Display(ref tilePanel, CurrentLayer, ShowGridLines, PanOffset);
                tilePanel.Update();
            }
            else
            {
                ShowGridLines = true;
                CurrentLevel.Display(ref tilePanel, CurrentLayer, ShowGridLines, PanOffset);
                tilePanel.Update();
            }
        }

        private void vScrollBarLevel_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollEventArgs s = e as ScrollEventArgs;
            PanOffset.Y = s.NewValue;
            lblScrollDebug.Text = string.Format("X: {0}, Y: {1} ", PanOffset.X, PanOffset.Y);
            CurrentLevel.Display(ref tilePanel, CurrentLayer, ShowGridLines, PanOffset);
            tilePanel.Update();
        }

        private void hScrollBarLevel_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollEventArgs s = e as ScrollEventArgs;
            PanOffset.X = s.NewValue;
            lblScrollDebug.Text = string.Format("X: {0}, Y: {1} ", PanOffset.X, PanOffset.Y);
            CurrentLevel.Display(ref tilePanel, CurrentLayer, ShowGridLines, PanOffset);
            tilePanel.Update();
        }
    }
    public class Tile
    {
        public Point Location;
        public Image Texture;
        public bool ShowGridLines;
        public Tile(Point l, Image i)
        {
            Location = l;
            Texture = i;
        }
        public void SetLocation(Point l)
        {
            Location = l;
        }
        public void SetTexture(Image i)
        {
            Texture = i;
        }
        public void Draw(Graphics g)
        {
            g.DrawImage(Texture, Location);
            if (ShowGridLines)
            {
                GraphicsUnit f = GraphicsUnit.Pixel;
                PointF[] points = {
                    new PointF(Texture.GetBounds(ref f).Top, Texture.GetBounds(ref f).Left),
                    new PointF(Texture.GetBounds(ref f).Top, Texture.GetBounds(ref f).Right),
                    new PointF(Texture.GetBounds(ref f).Bottom, Texture.GetBounds(ref f).Right),
                    new PointF(Texture.GetBounds(ref f).Bottom, Texture.GetBounds(ref f).Left)
                    };
                g.DrawLines(Pens.Black, points);
            }
        }
    }
    public class TileGrid
    {
        public List<Tile> Tiles;
        Bitmap ComposedImage;
        public TileGrid(List<Tile> t)
        {
            Tiles = t;
        }
        public void Composite()
        {
            int Width = 0, Height = 0;
            foreach (Tile t in Tiles)
            {
                if (t.Location.X > Width) Width = t.Location.X;
                if (t.Location.Y > Height) Height = t.Location.Y;
            }
            Bitmap b = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics canvas = Graphics.FromImage(b))
            {
                canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                foreach(Tile j in Tiles)
                {
                    canvas.DrawImage(j.Texture, j.Location);
                }
            }
            ComposedImage = b;
        }
        public void Draw(Graphics g, Point PanOffset)
        {
            g.DrawImage(ComposedImage, PanOffset);
        }
    }
    public enum LayerMode
    {
        Foreground,
        Midground,
        Background,
        Combined
    }
    public enum Tool
    {
        Pan,
        Draw,
        Erase,
        Edit,
        SelectBox,
        SelectIndividual
    }

    public class Level
    {
        public List<string> DependantTextures; //load textures from this list of strings
        public TileGrid fTileGrid;
        public TileGrid mTileGrid;
        public TileGrid bTileGrid;
        public TileGrid currentTileGrid;
        public Size TileGridDimensions;
        public int LevelNumber; //which level is it

        /// <summary>
        /// Constructor for a level
        /// </summary>
        /// <param name="Level Size (in pixels"></param>
        /// <param name="Level ID"></param>
        /// <param name="Foreground Tiles"></param>
        /// <param name="Midground Tiles"></param>
        /// <param name="Background Tiles"></param>
        public Level(Size gridSize, int place, 
            TileGrid f, TileGrid m, TileGrid b)
        {
            fTileGrid = f;
            mTileGrid = m;
            bTileGrid = b;
            
            TileGridDimensions = gridSize;
            LevelNumber = place;
            
            currentTileGrid = mTileGrid;
        }
        public void Display(ref Panel f, LayerMode layer, bool gridlines, Point panOffset)
        {
            f.CreateGraphics().Clear(Color.White);

            TileGridDimensions.Width += panOffset.X;
            TileGridDimensions.Height -= panOffset.Y;

            if (layer == LayerMode.Foreground)
            {
                currentTileGrid = fTileGrid;
            }
            else if (layer == LayerMode.Midground)
            {
                currentTileGrid = mTileGrid;
            }
            else if (layer == LayerMode.Background)
            {
                currentTileGrid = bTileGrid;
            }
            if (layer != LayerMode.Combined)
            {
                foreach (Tile p in currentTileGrid.Tiles)
                {
                    p.ShowGridLines = gridlines;
                }
                currentTileGrid.Composite();
                currentTileGrid.Draw(f.CreateGraphics(), panOffset);
            }
            else
            {
                //TODO: Combine all layers
            }
            TileGridDimensions.Width -= panOffset.X;
            TileGridDimensions.Height += panOffset.Y;
        }
    }
}