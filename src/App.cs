using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace RoxyPad {
 public class App : Application {
  static readonly string[] SupportedAudioExtensions={".mp3",".wav",".m4a",".aac",".wma"};
  Window win; Library lib; Forms.NotifyIcon tray; LocalAudio player=new LocalAudio(); BridgeControl bridge; Clip activeClip; double lastNonZeroVolume=0.5;
  bool favorites,quitting,updating,importing,isPlaying,isOpening,playerReady,playbackEnded,micRegistered,micReady; string libraryOverride; DispatcherTimer timer,micTimer,volumeSave,gainSave,gateSave; Mutex mutex; EventWaitHandle wake; RegisteredWaitHandle wait;
  T Get<T>(string name) where T:class {return win.FindName(name) as T;}
  static Brush Color(string color){return (Brush)new BrushConverter().ConvertFromString(color);}
  [STAThread] public static void Main(string[] args){
   if(args.Length>0&&args[0]=="--self-test"){Tests.Run(args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"RoxyPad-tests"));return;}
   var app=new App();if(args.Length==2&&args[0]=="--library")app.libraryOverride=Path.GetFullPath(args[1]); bool first;app.mutex=new Mutex(true,"Local\\RoxyPad-v01",out first);
   app.wake=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\RoxyPad-open-v01");
   if(!first){app.wake.Set();app.wake.Dispose();app.mutex.Dispose();return;}
   try{app.Start();app.Run();}catch(Exception ex){MessageBox.Show(ex.Message,"RoxyPad não pôde iniciar");}finally{if(app.micTimer!=null)app.micTimer.Stop();if(app.bridge!=null)app.bridge.Dispose();if(app.tray!=null)app.tray.Dispose();if(app.wait!=null)app.wait.Unregister(null);app.player.Close();app.wake.Dispose();app.mutex.ReleaseMutex();app.mutex.Dispose();}
  }
  void Start(){
   ShutdownMode=ShutdownMode.OnExplicitShutdown;
   lib=new Library(libraryOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RoxyPad"));
   bridge=new BridgeControl();bridge.Start();micRegistered=true;
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Main.xaml"))win=(Window)XamlReader.Load(stream);
   var fontFolder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets");
   try{win.FontFamily=new FontFamily(new Uri(fontFolder+Path.DirectorySeparatorChar,UriKind.Absolute),"#Poppins");}catch{win.FontFamily=new FontFamily("Segoe UI");}
   var appIcon=LoadIcon();win.Icon=Imaging.CreateBitmapSourceFromHIcon(appIcon.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());
   MainWindow=win;
   wait=ThreadPool.RegisterWaitForSingleObject(wake,delegate{Dispatcher.BeginInvoke(new Action(ShowWindow));},null,-1,false);
   Get<Grid>("TitleBar").MouseLeftButtonDown+=(s,e)=>{if(e.OriginalSource is TextBlock || e.OriginalSource is Grid || e.OriginalSource is Border)win.DragMove();};
   Get<Button>("Hide").Click+=(s,e)=>win.Close();
   win.Closing+=(s,e)=>{if(!quitting){e.Cancel=true;win.Hide();}};
   Get<Button>("Warning").Click+=(s,e)=>MessageBox.Show(win,MicDetails(),"Saída no microfone",MessageBoxButton.OK,MessageBoxImage.Information);
   Get<Button>("Add").Click+=(s,e)=>Pick();Get<Button>("PlayPause").Click+=(s,e)=>TogglePlayback();
   Get<Button>("All").Click+=(s,e)=>{favorites=false;Render();};Get<Button>("Favorites").Click+=(s,e)=>{favorites=true;Render();};
   Get<TextBox>("Search").TextChanged+=(s,e)=>Render();
   win.PreviewDragEnter+=(s,e)=>UpdateDropTarget(e);
   win.PreviewDragOver+=(s,e)=>UpdateDropTarget(e);
   win.DragLeave+=(s,e)=>{var p=e.GetPosition(win);if(p.X<0||p.Y<0||p.X>win.ActualWidth||p.Y>win.ActualHeight)Get<Border>("DropOverlay").Visibility=Visibility.Collapsed;};
   win.Drop+=(s,e)=>{Get<Border>("DropOverlay").Visibility=Visibility.Collapsed;var files=e.Data.GetData(DataFormats.FileDrop) as string[];if(files!=null)ImportFiles(files.Where(IsSupportedAudio).ToArray());};
   win.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape)Stop();};
   double savedVolume=0.5;string volumeFile=Path.Combine(lib.Root,"volume.txt");
   if(File.Exists(volumeFile)){double parsed;if(double.TryParse(File.ReadAllText(volumeFile),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out parsed)&&!double.IsNaN(parsed)&&parsed>=0&&parsed<=1)savedVolume=parsed;}
   lastNonZeroVolume=savedVolume>0?savedVolume:0.5;player.Volume=savedVolume;bridge.SetVolume(savedVolume);Get<Slider>("Volume").Value=savedVolume;UpdateVolumeLabel();
   var volumeHover=Get<Border>("VolumeHover");volumeHover.MouseEnter+=(s,e)=>{volumeHover.Width=178;volumeHover.Background=Color("#241F32");volumeHover.BorderBrush=Color("#725B8B");volumeHover.BorderThickness=new Thickness(1);Get<Slider>("Volume").Visibility=Visibility.Visible;Get<TextBlock>("VolumeLabel").Visibility=Visibility.Visible;};volumeHover.MouseLeave+=(s,e)=>{volumeHover.Width=36;volumeHover.Background=Brushes.Transparent;volumeHover.BorderBrush=Brushes.Transparent;volumeHover.BorderThickness=new Thickness(0);Get<Slider>("Volume").Visibility=Visibility.Collapsed;Get<TextBlock>("VolumeLabel").Visibility=Visibility.Collapsed;};
   Get<Button>("VolumeButton").Click+=(s,e)=>{var slider=Get<Slider>("Volume");slider.Value=slider.Value>0?0:(lastNonZeroVolume>0?lastNonZeroVolume:0.5);};
   volumeSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};volumeSave.Tick+=(s,e)=>{volumeSave.Stop();SaveVolume();};
   Get<Slider>("Volume").ValueChanged+=(s,e)=>{player.Volume=e.NewValue;bridge.SetVolume(e.NewValue);if(e.NewValue>0)lastNonZeroVolume=e.NewValue;UpdateVolumeLabel();volumeSave.Stop();volumeSave.Start();};
   double savedGain=4;string gainFile=Path.Combine(lib.Root,"mic-gain.txt");
   if(File.Exists(gainFile)){double parsed;if(double.TryParse(File.ReadAllText(gainFile),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out parsed)&&parsed>=1&&parsed<=8)savedGain=parsed;}
   bridge.SetMicGain(savedGain);Get<Slider>("MicGain").Value=savedGain;Get<TextBlock>("MicGainLabel").Text=Math.Round(savedGain*100)+"%";
   gainSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};gainSave.Tick+=(s,e)=>{gainSave.Stop();try{File.WriteAllText(gainFile,Get<Slider>("MicGain").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));}catch(IOException){}catch(UnauthorizedAccessException){}};
   string denoiseFile=Path.Combine(lib.Root,"isolar-voz.txt");bool denoiseOn=false;try{denoiseOn=File.Exists(denoiseFile)&&File.ReadAllText(denoiseFile).Trim()=="1";}catch(IOException){}catch(UnauthorizedAccessException){}
   Action showDenoise=()=>{var b=Get<Button>("Denoise");b.Background=Color(denoiseOn?"#6C4BA1":"#292536");b.BorderBrush=Color(denoiseOn?"#B795F5":"#493F57");Get<Slider>("Gate").IsEnabled=denoiseOn;};
   string gateFile=Path.Combine(lib.Root,"portao.txt");double savedGate=0.6;
   if(File.Exists(gateFile)){double parsed;if(double.TryParse(File.ReadAllText(gateFile),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out parsed)&&parsed>=0&&parsed<=0.95)savedGate=parsed;}
   Get<Slider>("Gate").Value=savedGate;Get<TextBlock>("GateLabel").Text=Math.Round(savedGate*100)+"%";bridge.SetGate(savedGate);
   gateSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};gateSave.Tick+=(s,e)=>{gateSave.Stop();try{File.WriteAllText(gateFile,Get<Slider>("Gate").Value.ToString(System.Globalization.CultureInfo.InvariantCulture));}catch(IOException){}catch(UnauthorizedAccessException){}};
   Get<Slider>("Gate").ValueChanged+=(s,e)=>{bridge.SetGate(e.NewValue);Get<TextBlock>("GateLabel").Text=Math.Round(e.NewValue*100)+"%";gateSave.Stop();gateSave.Start();};
   bridge.SetDenoise(denoiseOn);showDenoise();
   Get<Button>("Denoise").Click+=(s,e)=>{denoiseOn=!denoiseOn;bridge.SetDenoise(denoiseOn);showDenoise();try{File.WriteAllText(denoiseFile,denoiseOn?"1":"0");}catch(IOException){}catch(UnauthorizedAccessException){}};
   Get<Slider>("MicGain").ValueChanged+=(s,e)=>{bridge.SetMicGain(e.NewValue);Get<TextBlock>("MicGainLabel").Text=Math.Round(e.NewValue*100)+"%";gainSave.Stop();gainSave.Start();};
   Get<Slider>("Seek").ValueChanged+=(s,e)=>{if(!updating&&player.NaturalDuration.HasTimeSpan)player.Position=TimeSpan.FromSeconds(e.NewValue);};
   player.MediaOpened+=(s,e)=>{playerReady=true;Get<Slider>("Seek").Maximum=player.NaturalDuration.HasTimeSpan?Math.Max(1,player.NaturalDuration.TimeSpan.TotalSeconds):1;Get<Slider>("Seek").IsEnabled=player.NaturalDuration.HasTimeSpan;if(isOpening){player.Play();isPlaying=true;bridge.SetActive(true);isOpening=false;playbackEnded=false;}UpdatePlayerUi();};
   player.MediaEnded+=(s,e)=>{isPlaying=false;bridge.SetActive(false);playbackEnded=true;UpdatePlayerUi();};player.MediaFailed+=(s,e)=>{isOpening=false;playerReady=false;isPlaying=false;bridge.SetActive(false);Get<TextBlock>("PlayerState").Text="Não foi possível reproduzir este arquivo";UpdatePlayerUi();MessageBox.Show(win,"O Windows não conseguiu reproduzir este arquivo. Ele pode estar corrompido ou usar um codec não disponível.\n\n"+e.ErrorException.Message,"Falha de reprodução");};
   timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(s,e)=>{if(!playerReady||activeClip==null)return;updating=true;Get<Slider>("Seek").Value=player.Position.TotalSeconds;Get<TextBlock>("Time").Text=Stamp(player.Position)+" / "+(player.NaturalDuration.HasTimeSpan?Stamp(player.NaturalDuration.TimeSpan):"…");updating=false;};timer.Start();
   micTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};micTimer.Tick+=(s,e)=>{micReady=bridge.Refresh();micRegistered=!bridge.CableMissing;UpdatePlayerUi();Get<Button>("Warning").ToolTip=MicDetails();if(tray!=null)tray.Text=micReady?"RoxyPad · microfone pronto":"RoxyPad · "+(micRegistered?"microfone offline":"instale o VB-CABLE");};micTimer.Start();bridge.Refresh();
   tray=new Forms.NotifyIcon{Icon=appIcon,Text="RoxyPad · reprodução local",Visible=true};
   var menu=new Forms.ContextMenuStrip();menu.Items.Add("Abrir RoxyPad",null,(s,e)=>Dispatcher.Invoke(new Action(ShowWindow)));menu.Items.Add("Parar áudio",null,(s,e)=>Dispatcher.Invoke(new Action(Stop)));menu.Items.Add("Sair",null,(s,e)=>Dispatcher.Invoke(new Action(()=>{if(importing){MessageBox.Show(win,"Aguarde a importação terminar antes de sair.");return;}quitting=true;timer.Stop();Stop();tray.Visible=false;win.Close();Shutdown();})));tray.ContextMenuStrip=menu;
   tray.MouseClick+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left)Dispatcher.Invoke(new Action(ShowWindow));};
   Exit+=(s,e)=>{volumeSave.Stop();if(micTimer!=null)micTimer.Stop();bridge.SetActive(false);SaveVolume();};Get<Button>("Warning").ToolTip=MicDetails();Render();win.Show();
  }
  void ShowWindow(){win.Show();win.WindowState=WindowState.Normal;win.Activate();}
  string MicDetails(){if(!micRegistered)return "O som toca só neste PC. Instale o VB-CABLE (vb-audio.com/Cable) para enviar os áudios ao microfone.";if(!micReady)return "O canal de áudio não está pronto: "+(bridge==null?"indisponível":bridge.Error??"aguardando");return (isPlaying?"Áudio tocando e sendo enviado junto com a sua voz.":"Sua voz está passando pelo RoxyPad.")+"\nNo Discord, escolha \"CABLE Output\" como dispositivo de entrada. Mantenha o RoxyPad aberto.";}
  void UpdateVolumeLabel(){bool muted=player.Volume==0;Get<TextBlock>("VolumeLabel").Text=muted?"Mudo":Math.Round(player.Volume*100)+"%";Get<System.Windows.Shapes.Path>("VolumeWaves").Data=Geometry.Parse(muted?"M 10,6 L 14,10 M 14,6 L 10,10":"M 10,5 C 11.5,6 11.5,10 10,11 M 12,3 C 14.5,5 14.5,11 12,13");}
  void SaveVolume(){try{string path=Path.Combine(lib.Root,"volume.txt"),temp=path+".tmp";File.WriteAllText(temp,player.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch(IOException){Get<TextBlock>("PlayerState").Text="Volume aplicado, mas a preferência não foi salva";}catch(UnauthorizedAccessException){Get<TextBlock>("PlayerState").Text="Volume aplicado, mas a preferência não foi salva";}}
  System.Drawing.Icon LoadIcon(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RoxyPad.ico")){if(stream==null)return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();using(var icon=new System.Drawing.Icon(stream))return (System.Drawing.Icon)icon.Clone();}}
  static string Stamp(TimeSpan t){return t.TotalHours>=1?((int)t.TotalHours)+t.ToString(@"\:mm\:ss"):((int)t.TotalMinutes)+t.ToString(@"\:ss");}
  static bool IsSupportedAudio(string path){return !string.IsNullOrEmpty(path)&&SupportedAudioExtensions.Contains(Path.GetExtension(path),StringComparer.OrdinalIgnoreCase);}
  void UpdateDropTarget(DragEventArgs e){var files=e.Data.GetData(DataFormats.FileDrop) as string[];bool accepts=!importing&&files!=null&&files.Any(IsSupportedAudio);e.Effects=accepts?DragDropEffects.Copy:DragDropEffects.None;Get<Border>("DropOverlay").Visibility=accepts?Visibility.Visible:Visibility.Collapsed;e.Handled=true;}
  void UpdatePlayerUi(){
   Get<TextBlock>("Now").Text=activeClip==null?"Escolha um áudio":activeClip.Name;
   Get<TextBlock>("PlayerState").Text=isOpening?"Abrindo áudio…":isPlaying?(micRegistered&&micReady?"Tocando no microfone":"Tocando neste PC · microfone indisponível"):playbackEnded?"Reprodução concluída":activeClip!=null&&playerReady?"Pausado · microfone silenciado":activeClip==null?"Pronto para tocar":(micRegistered&&micReady?"Microfone pronto":"Som local pronto");
   var button=Get<Button>("PlayPause");button.Content=isPlaying?"Ⅱ":"▶";button.ToolTip=isPlaying?"Pausar":"Reproduzir";button.IsEnabled=activeClip!=null&&!isOpening;
  }
  void Stop(){bridge.SetActive(false);player.Close();isOpening=false;playerReady=false;isPlaying=false;playbackEnded=false;updating=true;Get<Slider>("Seek").Value=0;Get<Slider>("Seek").IsEnabled=false;updating=false;Get<TextBlock>("Time").Text="0:00 / 0:00";UpdatePlayerUi();Render();}
  void Play(Clip c){if(!File.Exists(lib.AudioPath(c))){MessageBox.Show(win,"O arquivo não está mais na biblioteca. Importe novamente.");return;}bridge.SetActive(false);player.Close();activeClip=c;isOpening=true;playerReady=false;isPlaying=false;playbackEnded=false;updating=true;Get<Slider>("Seek").Value=0;updating=false;Get<Slider>("Seek").IsEnabled=false;Get<TextBlock>("Time").Text="0:00 / 0:00";UpdatePlayerUi();player.Open(new Uri(lib.AudioPath(c)));Render();}
  void TogglePlayback(){if(activeClip==null||isOpening)return;if(!playerReady){Play(activeClip);return;}if(isPlaying){player.Pause();isPlaying=false;bridge.SetActive(false);}else{if(playbackEnded||(player.NaturalDuration.HasTimeSpan&&player.Position>=player.NaturalDuration.TimeSpan)){player.Position=TimeSpan.Zero;playbackEnded=false;}player.Play();isPlaying=true;bridge.SetActive(true);}UpdatePlayerUi();}
  void Pick(){var d=new Microsoft.Win32.OpenFileDialog{Multiselect=true,Filter="Áudios|*.mp3;*.wav;*.m4a;*.aac;*.wma",Title="Adicionar à biblioteca"};if(d.ShowDialog(win)==true)ImportFiles(d.FileNames);}
  async void ImportFiles(string[] paths){
   if(importing)return;importing=true;Get<Button>("Add").IsEnabled=false;Get<StackPanel>("Rows").IsEnabled=false;
   var errors=new System.Collections.Generic.List<string>();Get<TextBlock>("PlayerState").Text="Importando arquivos…";
   // Only file copying and index writes happen off the UI thread; library mutations are serialized while importing.
   foreach(var path in paths){try{await Task.Run(()=>lib.Import(path));}catch(Exception ex){errors.Add(Path.GetFileName(path)+": "+ex.Message);}}
   importing=false;Get<Button>("Add").IsEnabled=true;Get<StackPanel>("Rows").IsEnabled=true;UpdatePlayerUi();Render();
   if(errors.Count>0)MessageBox.Show(win,string.Join("\n",errors.Take(8)),"Alguns arquivos não foram importados");
  }
  void Render(){
   if(win==null||importing)return;
   var query=Get<TextBox>("Search").Text;Get<TextBlock>("SearchHint").Visibility=query.Length==0?Visibility.Visible:Visibility.Collapsed;
   Get<Button>("All").Background=Color(favorites?"#292632":"#6C4BA1");Get<Button>("Favorites").Background=Color(favorites?"#6C4BA1":"#292632");
   var rows=Get<StackPanel>("Rows");rows.Children.Clear();var clips=lib.Find(query,favorites).ToList();
   Get<TextBlock>("Count").Text="SUA BIBLIOTECA · "+clips.Count+(clips.Count!=lib.Clips.Count?" de "+lib.Clips.Count:"");
   Get<StackPanel>("Empty").Visibility=clips.Count==0?Visibility.Visible:Visibility.Collapsed;Get<TextBlock>("EmptyHint").Text=lib.Clips.Count==0?"Adicione um áudio para começar.":"Nenhum áudio neste filtro.";
   foreach(var c in clips){
    var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(28)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(32)});
    var symbol=new TextBlock{Text=activeClip==c&&isPlaying?"♫":"▶",Foreground=Color("#C5A6FF"),VerticalAlignment=VerticalAlignment.Center};grid.Children.Add(symbol);
    var stack=new StackPanel();stack.Children.Add(new TextBlock{Text=c.Name,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=13,FontWeight=FontWeights.Bold});stack.Children.Add(new TextBlock{Text=Path.GetExtension(c.FileName).TrimStart('.').ToUpperInvariant()+"  ·  reprodução local",Foreground=Color("#AAA0B8"),FontSize=10,Margin=new Thickness(0,4,0,0)});Grid.SetColumn(stack,1);grid.Children.Add(stack);
    var star=new TextBlock{Text=c.Favorite?"★":"",Foreground=Color("#B69CFF"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(star,2);grid.Children.Add(star);
    var button=new Button{Content=grid,HorizontalContentAlignment=HorizontalAlignment.Stretch,Background=Color(activeClip==c?"#352849":"#24212D"),BorderBrush=Color(activeClip==c?"#9270C2":"#383140"),Margin=new Thickness(0,0,0,6),Padding=new Thickness(12,10,12,10),ToolTip=c.Name};
    button.Click+=(s,e)=>Play(c);
    var menu=new ContextMenu();Menu(menu,"Renomear",()=>Rename(c));Menu(menu,c.Favorite?"Remover dos favoritos":"Favoritar",()=>{lib.ToggleFavorite(c);Render();});Menu(menu,"Salvar cópia em Downloads",()=>{var p=lib.Export(c,Downloads());MessageBox.Show(win,"Cópia salva em:\n"+p,"Áudio exportado");});menu.Items.Add(new Separator());Menu(menu,"Remover da lista",()=>{if(MessageBox.Show(win,"Remover \""+c.Name+"\" da lista? Seu arquivo original será preservado.","Remover áudio",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;if(activeClip==c){Stop();activeClip=null;UpdatePlayerUi();}lib.Remove(c);Render();});button.ContextMenu=menu;rows.Children.Add(button);
   }
  }
  void Menu(ContextMenu menu,string label,Action action){var item=new MenuItem{Header=label};item.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(win,ex.Message,"Não foi possível concluir");}};menu.Items.Add(item);}
  void Rename(Clip c){var dialog=new Window{Owner=win,Title="Renomear áudio",Width=340,Height=165,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=Color("#17151D"),Foreground=Brushes.White};var panel=new StackPanel{Margin=new Thickness(16)};var input=new TextBox{Text=c.Name,MaxLength=120,FontSize=14,Padding=new Thickness(6)};var save=new Button{Content="Salvar",Margin=new Thickness(0,12,0,0),Padding=new Thickness(8),IsDefault=true};save.Click+=(s,e)=>{try{lib.Rename(c,input.Text);dialog.DialogResult=true;}catch(Exception ex){MessageBox.Show(dialog,ex.Message);}};panel.Children.Add(input);panel.Children.Add(save);dialog.Content=panel;dialog.Loaded+=(s,e)=>{input.Focus();input.SelectAll();};dialog.ShowDialog();if(activeClip==c)UpdatePlayerUi();Render();}
  [DllImport("shell32.dll")] static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)]Guid id,uint flags,IntPtr token,out IntPtr path);
  static string Downloads(){IntPtr p;int result=SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"),0,IntPtr.Zero,out p);if(result!=0)Marshal.ThrowExceptionForHR(result);try{return Marshal.PtrToStringUni(p);}finally{Marshal.FreeCoTaskMem(p);}}
 }
}
