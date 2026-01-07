namespace MyPlayer
{
    partial class frmMyPlayer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMyPlayer));
            label1 = new Label();
            txtPathMusicas = new TextBox();
            btnOpenFolderMusics = new Button();
            imageList1 = new ImageList(components);
            treeView1 = new TreeView();
            listView1 = new ListView();
            contextMenuStrip1 = new ContextMenuStrip(components);
            btnRandomizar = new Button();
            btnVoltar = new Button();
            btnPlayPause = new Button();
            btnProximo = new Button();
            lblStatus = new Label();
            progressBar1 = new ProgressBar();
            trackBar1 = new TrackBar();
            pictureBox1 = new PictureBox();
            txtFiltro = new TextBox();
            notifyIcon1 = new NotifyIcon(components);
            pictureBox2 = new PictureBox();
            btnCarregarMusicasPlayList = new Button();
            btnSalvarMusicasPlayList = new Button();
            btnExcluirMusicasPlayList = new Button();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(39, 14);
            label1.Name = "label1";
            label1.Size = new Size(75, 25);
            label1.TabIndex = 0;
            label1.Text = "Músicas";
            // 
            // txtPathMusicas
            // 
            txtPathMusicas.Location = new Point(129, 11);
            txtPathMusicas.Name = "txtPathMusicas";
            txtPathMusicas.Size = new Size(933, 31);
            txtPathMusicas.TabIndex = 1;
            // 
            // btnOpenFolderMusics
            // 
            btnOpenFolderMusics.ImageIndex = 8;
            btnOpenFolderMusics.ImageList = imageList1;
            btnOpenFolderMusics.Location = new Point(1068, 9);
            btnOpenFolderMusics.Name = "btnOpenFolderMusics";
            btnOpenFolderMusics.Size = new Size(45, 34);
            btnOpenFolderMusics.TabIndex = 2;
            btnOpenFolderMusics.UseVisualStyleBackColor = true;
            btnOpenFolderMusics.Click += btnOpenFolderMusics_Click;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "icons8-folder-32.png");
            imageList1.Images.SetKeyName(1, "icons8-open-file-folder-32.png");
            imageList1.Images.SetKeyName(2, "icons8-file-32.png");
            imageList1.Images.SetKeyName(3, "icons8-play-20.png");
            imageList1.Images.SetKeyName(4, "icons8-back-20.png");
            imageList1.Images.SetKeyName(5, "icons8-next-20.png");
            imageList1.Images.SetKeyName(6, "icons8-random-20.png");
            imageList1.Images.SetKeyName(7, "icons8-treble-clef-20.png");
            imageList1.Images.SetKeyName(8, "icons8-folder-20.png");
            imageList1.Images.SetKeyName(9, "icons8-pause-20.png");
            imageList1.Images.SetKeyName(10, "icons8-mp3-26.png");
            imageList1.Images.SetKeyName(11, "icons8-upload-20.png");
            imageList1.Images.SetKeyName(12, "icons8-save-20.png");
            imageList1.Images.SetKeyName(13, "icons8-remove-20.png");
            // 
            // treeView1
            // 
            treeView1.ImageIndex = 0;
            treeView1.ImageList = imageList1;
            treeView1.Location = new Point(13, 53);
            treeView1.Name = "treeView1";
            treeView1.SelectedImageIndex = 0;
            treeView1.Size = new Size(286, 347);
            treeView1.TabIndex = 3;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // listView1
            // 
            listView1.CheckBoxes = true;
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.FullRowSelect = true;
            listView1.LargeImageList = imageList1;
            listView1.Location = new Point(305, 53);
            listView1.Name = "listView1";
            listView1.Size = new Size(808, 347);
            listView1.SmallImageList = imageList1;
            listView1.TabIndex = 4;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.ColumnClick += listView1_ColumnClick;
            listView1.ItemChecked += listView1_ItemChecked;
            listView1.DoubleClick += listView1_DoubleClick;
            listView1.MouseDown += listView1_MouseDown;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // btnRandomizar
            // 
            btnRandomizar.ImageIndex = 6;
            btnRandomizar.ImageList = imageList1;
            btnRandomizar.Location = new Point(11, 497);
            btnRandomizar.Name = "btnRandomizar";
            btnRandomizar.Size = new Size(41, 34);
            btnRandomizar.TabIndex = 6;
            btnRandomizar.UseVisualStyleBackColor = true;
            btnRandomizar.Click += btnRandomizar_Click;
            // 
            // btnVoltar
            // 
            btnVoltar.ImageIndex = 4;
            btnVoltar.ImageList = imageList1;
            btnVoltar.Location = new Point(58, 497);
            btnVoltar.Name = "btnVoltar";
            btnVoltar.Size = new Size(41, 34);
            btnVoltar.TabIndex = 7;
            btnVoltar.UseVisualStyleBackColor = true;
            btnVoltar.Click += btnVoltar_Click;
            // 
            // btnPlayPause
            // 
            btnPlayPause.ImageIndex = 3;
            btnPlayPause.ImageList = imageList1;
            btnPlayPause.Location = new Point(105, 497);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(41, 34);
            btnPlayPause.TabIndex = 8;
            btnPlayPause.UseVisualStyleBackColor = true;
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // btnProximo
            // 
            btnProximo.ImageIndex = 5;
            btnProximo.ImageList = imageList1;
            btnProximo.Location = new Point(152, 497);
            btnProximo.Name = "btnProximo";
            btnProximo.Size = new Size(41, 34);
            btnProximo.TabIndex = 9;
            btnProximo.UseVisualStyleBackColor = true;
            btnProximo.Click += btnProximo_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(761, 502);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(60, 25);
            lblStatus.TabIndex = 10;
            lblStatus.Text = "Status";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(929, 497);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(182, 34);
            progressBar1.TabIndex = 11;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(211, 497);
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(520, 69);
            trackBar1.TabIndex = 12;
            trackBar1.Scroll += trackBar1_Scroll;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(11, 547);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1098, 75);
            pictureBox1.TabIndex = 13;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // txtFiltro
            // 
            txtFiltro.Location = new Point(11, 452);
            txtFiltro.Name = "txtFiltro";
            txtFiltro.Size = new Size(1098, 31);
            txtFiltro.TabIndex = 14;
            txtFiltro.TextChanged += txtFiltro_TextChanged;
            txtFiltro.KeyDown += txtFiltro_KeyDown;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "notifyIcon1";
            notifyIcon1.Visible = true;
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = Properties.Resources.icons8_treble_clef_20;
            pictureBox2.Location = new Point(13, 17);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(20, 20);
            pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox2.TabIndex = 15;
            pictureBox2.TabStop = false;
            // 
            // btnCarregarMusicasPlayList
            // 
            btnCarregarMusicasPlayList.ImageIndex = 11;
            btnCarregarMusicasPlayList.ImageList = imageList1;
            btnCarregarMusicasPlayList.Location = new Point(107, 406);
            btnCarregarMusicasPlayList.Name = "btnCarregarMusicasPlayList";
            btnCarregarMusicasPlayList.Size = new Size(41, 34);
            btnCarregarMusicasPlayList.TabIndex = 18;
            btnCarregarMusicasPlayList.UseVisualStyleBackColor = true;
            btnCarregarMusicasPlayList.Click += btnCarregarMusicasPlayList_Click;
            // 
            // btnSalvarMusicasPlayList
            // 
            btnSalvarMusicasPlayList.ImageIndex = 12;
            btnSalvarMusicasPlayList.ImageList = imageList1;
            btnSalvarMusicasPlayList.Location = new Point(60, 406);
            btnSalvarMusicasPlayList.Name = "btnSalvarMusicasPlayList";
            btnSalvarMusicasPlayList.Size = new Size(41, 34);
            btnSalvarMusicasPlayList.TabIndex = 17;
            btnSalvarMusicasPlayList.UseVisualStyleBackColor = true;
            btnSalvarMusicasPlayList.Click += btnSalvarMusicasPlayList_Click;
            // 
            // btnExcluirMusicasPlayList
            // 
            btnExcluirMusicasPlayList.Enabled = false;
            btnExcluirMusicasPlayList.ImageIndex = 13;
            btnExcluirMusicasPlayList.ImageList = imageList1;
            btnExcluirMusicasPlayList.Location = new Point(13, 406);
            btnExcluirMusicasPlayList.Name = "btnExcluirMusicasPlayList";
            btnExcluirMusicasPlayList.Size = new Size(41, 34);
            btnExcluirMusicasPlayList.TabIndex = 16;
            btnExcluirMusicasPlayList.UseVisualStyleBackColor = true;
            btnExcluirMusicasPlayList.Click += btnExcluirMusicasPlayList_Click;
            // 
            // frmMyPlayer
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1123, 643);
            Controls.Add(btnCarregarMusicasPlayList);
            Controls.Add(btnSalvarMusicasPlayList);
            Controls.Add(btnExcluirMusicasPlayList);
            Controls.Add(pictureBox2);
            Controls.Add(txtFiltro);
            Controls.Add(pictureBox1);
            Controls.Add(trackBar1);
            Controls.Add(progressBar1);
            Controls.Add(lblStatus);
            Controls.Add(btnProximo);
            Controls.Add(btnPlayPause);
            Controls.Add(btnVoltar);
            Controls.Add(btnRandomizar);
            Controls.Add(listView1);
            Controls.Add(treeView1);
            Controls.Add(btnOpenFolderMusics);
            Controls.Add(txtPathMusicas);
            Controls.Add(label1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "frmMyPlayer";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "My Player";
            FormClosing += frmMyPlayer_FormClosing;
            Load += frmMyPlayer_Load;
            Shown += frmMyPlayer_Shown;
            Resize += frmMyPlayer_Resize;
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtPathMusicas;
        private Button btnOpenFolderMusics;
        private TreeView treeView1;
        private ListView listView1;
        private ImageList imageList1;
        private Button btnRandomizar;
        private Button btnVoltar;
        private Button btnPlayPause;
        private Button btnProximo;
        private Label lblStatus;
        private ProgressBar progressBar1;
        private TrackBar trackBar1;
        private ContextMenuStrip contextMenuStrip1;
        private PictureBox pictureBox1;
        private TextBox txtFiltro;
        private NotifyIcon notifyIcon1;
        private PictureBox pictureBox2;
        private Button btnCarregarMusicasPlayList;
        private Button btnSalvarMusicasPlayList;
        private Button btnExcluirMusicasPlayList;
    }
}
