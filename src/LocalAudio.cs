using System;
using System.Windows;
using System.Windows.Media;

namespace RoxyPad {
 // MediaPlayer.Close resets its own volume to 0.5. Keep the user's choice
 // outside MediaPlayer and restore it whenever a source is closed/opened.
 public sealed class LocalAudio {
  readonly MediaPlayer player = new MediaPlayer();
  double volume = 0.5;
  public LocalAudio(){player.MediaOpened+=(s,e)=>{ApplyVolume();if(MediaOpened!=null)MediaOpened(this,e);};player.MediaEnded+=(s,e)=>{if(MediaEnded!=null)MediaEnded(this,e);};player.MediaFailed+=(s,e)=>{if(MediaFailed!=null)MediaFailed(this,e);};}
  public event EventHandler MediaOpened;
  public event EventHandler MediaEnded;
  public event EventHandler<ExceptionEventArgs> MediaFailed;
  public double Volume {get{return volume;}set{if(double.IsNaN(value)||double.IsInfinity(value))throw new ArgumentException("Volume inválido");volume=Math.Max(0,Math.Min(1,value));ApplyVolume();}}
  internal double AppliedVolume {get{return player.Volume;}}
  internal bool AppliedMute {get{return player.IsMuted;}}
  public Duration NaturalDuration {get{return player.NaturalDuration;}}
  public TimeSpan Position {get{return player.Position;}set{player.Position=value;}}
  void ApplyVolume(){player.Volume=volume;player.IsMuted=volume==0;}
  public void Open(Uri source){player.Open(source);ApplyVolume();}
  public void Close(){player.Close();ApplyVolume();}
  public void Play(){ApplyVolume();player.Play();}
  public void Pause(){player.Pause();}
 }
}
