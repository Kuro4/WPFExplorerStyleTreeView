using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFExplorerStyleTreeView
{
    /// <summary>
    /// ルートディレクトリ用クラス
    /// _RootDirectoryに追加したNodeがルートになる
    /// コンストラクタでは1つのルートだけ追加する
    /// ルートが複数になる場合はインスタンス化後にAdd()で追加する
    /// </summary>
    public class RootDirectoryNode
    {
        /// <summary>
        /// ルートNodeのリスト
        /// こいつをバインドする
        /// </summary>
        public List<BaseNode> _RootDirectory { get; private set; } = new List<BaseNode>();

        public RootDirectoryNode(DirectoryInfo rootDirectory, TreeViewHelper helper)
        {
            _RootDirectory.Add(new DirectoryNode(rootDirectory, helper));
        }
    }

    /// <summary>
    /// 各情報を参照型として共有するためのヘルパークラス
    /// </summary>
    public class TreeViewHelper
    {
        /// <summary>
        /// ファイル検索時のフィルター
        /// </summary>
        public string _SearchPattern { get; set; } = "*";
        /// <summary>
        /// Headerに表示するアイコンの幅
        /// </summary>
        public double _ImageWidth { get; set; } = 18;
        /// <summary>
        /// Headerに表示するアイコンの高さ
        /// </summary>
        public double _ImageHeight { get; set; } = 15;
        /// <summary>
        /// フォルダ未展開時にHeaderに表示するアイコン
        /// </summary>
        public ImageSource _CloseFolderIcon { get; set; } = new BitmapImage();
        /// <summary>
        /// フォルダ展開時にHeaderに表示するアイコン
        /// </summary>
        public ImageSource _OpenFolderIcon { get; set; } = new BitmapImage();
        /// <summary>
        /// ファイルのHeaderに表示するアイコン
        /// </summary>
        public ImageSource _FileIcon { get; set; } = new BitmapImage();

        public TreeViewHelper(ImageSource closeFolderIcon, ImageSource openFolderIcon, ImageSource fileIcon, string searchPattern = "*", double imageWidth = 18, double imageHeight = 15)
        {
            _SearchPattern = searchPattern;
            _ImageWidth = imageWidth;
            _ImageHeight = imageHeight;
            _CloseFolderIcon = closeFolderIcon;
            _OpenFolderIcon = openFolderIcon;
            _FileIcon = fileIcon;
        }
    }

    /// <summary>
    /// TreeViewItemを継承し、Nodeのベースとなるクラス
    /// </summary>
    public class BaseNode : TreeViewItem
    {
        /// <summary>
        /// 自身が持つFileSystemInfo(DirectoryInfoかFileInfo)
        /// </summary>
        public FileSystemInfo _Info { get; set; }
        /// <summary>
        /// Headerに表示するアイコン
        /// </summary>
        public Image _HeaderImage { get; private set; } = new Image();
        /// <summary>
        /// Headerに表示するテキスト
        /// </summary>
        public TextBlock _HeaderText { get; private set; } = new TextBlock();
        /// <summary>
        /// Headerに表示するアイコンとテキストを持つパネル
        /// </summary>
        public StackPanel _HeaderPanel { get; private set; } = new StackPanel() { Orientation = Orientation.Horizontal };
        public BaseNode()
        {
            _HeaderPanel.Children.Add(_HeaderImage);
            _HeaderPanel.Children.Add(_HeaderText);
            this.Header = _HeaderPanel;
        }
    }

    /// <summary>
    /// Directory用のNode
    /// </summary>
    public class DirectoryNode : BaseNode
    {
        /// <summary>
        /// 共有のためのTreeViewHelper
        /// </summary>
        public TreeViewHelper Helper;
        /// <summary>
        /// 1度でも展開したかどうか
        /// </summary>
        private bool hasExpandedOnce = false;

        /// <summary>
        /// 自身のDirectory内にサブDirectoryか_SearchPatternに一致するファイルがあれば、
        /// 展開できることを表示するためにダミーノードを追加する
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="helper"></param>
        public DirectoryNode(DirectoryInfo dir, TreeViewHelper helper)
        {
            this._Info = dir;
            this.Helper = helper;
            this._HeaderImage.Source = Helper._CloseFolderIcon;
            this._HeaderImage.Width = Helper._ImageWidth;
            this._HeaderImage.Height = Helper._ImageHeight;
            this._HeaderText.Text = dir.Name;

            if (dir.Exists)
            {
                try
                {
                    if (dir.EnumerateDirectories().Any() || dir.EnumerateFiles(Helper._SearchPattern).Any())
                    {
                        this.Items.Add(new BaseNode());//ダミーノードの追加
                    }
                }
                //アクセス拒否、ディレクトリ・ファイルが見つからないエラーをスキップ
                catch (Exception e) when (e is UnauthorizedAccessException || e is DirectoryNotFoundException || e is FileNotFoundException)
                {
                    Console.WriteLine(e.Source + "：" + e.Message);
                }
            }
            this.Expanded += DirNode_Expanded;
            this.Collapsed += DirNode_Collapsed;
        }

        /// <summary>
        /// 展開した時、子ノードがあれば_HeaderImageを_OpenFolderIconに変更する
        /// また、はじめて展開した時ならサブDirectoryとFileを探査して子ノードに追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirNode_Expanded(object sender, RoutedEventArgs e)
        {
            var directory = (DirectoryInfo)this._Info;

            if (this.Items.Count > 0)
            {
                this._HeaderImage.Source = this.Helper._OpenFolderIcon;
                if (!hasExpandedOnce)
                {
                    this.Items.Clear();
                    foreach (var dir in directory.GetDirectories())
                    {
                        this.Items.Add(new DirectoryNode(dir, Helper));
                    }
                    foreach (var file in directory.GetFiles(Helper._SearchPattern))
                    {
                        this.Items.Add(new FileNode(file, Helper));
                    }
                    hasExpandedOnce = true;
                }
            }
        }

        /// <summary>
        /// 展開を閉じた時、_HeaderImageを_CloseFolderIconに変更する
        /// このイベントは親Nodeまで伝播するのでIsExpandedプロパティで判定する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirNode_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!this.IsExpanded) this._HeaderImage.Source = this.Helper._CloseFolderIcon;
        }
    }

    /// <summary>
    /// File用のNode
    /// </summary>
    public class FileNode : BaseNode
    {
        public FileNode(FileInfo file, TreeViewHelper helper)
        {
            this._Info = file;
            this._HeaderImage.Source = helper._FileIcon;
            this._HeaderImage.Width = helper._ImageWidth;
            this._HeaderImage.Height = helper._ImageHeight;
            this._HeaderText.Text = file.Name;
        }
    }
}
