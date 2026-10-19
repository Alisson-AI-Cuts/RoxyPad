using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace RoxyPad {
 sealed class BridgeControl : IDisposable {
  const string MappingName="Local\\RoxyPad.AudioBridge.v1";
  const int Magic=0x59584F52, Version=1, StatusReady=2;
  Process process;
  MemoryMappedFile mapping;
  MemoryMappedViewAccessor view;
  bool requestedActive, disposed;
  int volumePermille=500, micGainPercent=400, denoise=0, gatePercent=0;
  public bool Ready {get;private set;}
  public string Error {get;private set;}
  // Set by RoxyBridge when no "CABLE Input" endpoint (VB-CABLE) exists.
  public bool CableMissing {get;private set;}


  public void Start(){
   if(disposed)throw new ObjectDisposedException("BridgeControl");
   string executable=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"RoxyBridge.exe");
   if(!File.Exists(executable)){Error="Canal de áudio não encontrado";return;}
   try{
    process=Process.Start(new ProcessStartInfo{FileName=executable,Arguments=Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
    if(process==null)Error="Não foi possível iniciar o canal de áudio";
   }catch(Exception ex){Error="Não foi possível iniciar o canal de áudio: "+ex.Message;}
  }

  public void SetActive(bool active){requestedActive=active;Apply();}
  public void SetVolume(double value){
   if(Double.IsNaN(value)||Double.IsInfinity(value))return;
   volumePermille=(int)Math.Max(0,Math.Min(1000,Math.Round(value*1000)));
   Apply();
  }

  public void SetMicGain(double value){
   if(Double.IsNaN(value)||Double.IsInfinity(value))return;
   micGainPercent=(int)Math.Max(0,Math.Min(1000,Math.Round(value*100)));
   Apply();
  }

  public void SetDenoise(bool on){denoise=on?1:0;Apply();}
  public void SetGate(double value){if(Double.IsNaN(value)||Double.IsInfinity(value))return;gatePercent=(int)Math.Max(0,Math.Min(100,Math.Round(value*100)));Apply();}

  public bool Refresh(){
   Ready=false;
   if(disposed)return false;
   if(process!=null){
    // RoxyBridge.exe relaunches itself outside RoxyPad's process tree and exits with 0.
    try{if(process.HasExited&&process.ExitCode!=0){Error="Canal de áudio encerrou com código "+process.ExitCode;return false;}}
    catch(InvalidOperationException){Error="Canal de áudio indisponível";return false;}
   }
   try{
    if(view==null){
     mapping=MemoryMappedFile.OpenExisting(MappingName,MemoryMappedFileRights.ReadWrite);
     view=mapping.CreateViewAccessor(0,64,MemoryMappedFileAccess.ReadWrite);
    }
    if(view.ReadInt32(0)!=Magic||view.ReadInt32(4)!=Version){Error="Versão do canal de áudio incompatível";return false;}
    int status=view.ReadInt32(28);
    Ready=status==StatusReady;
    CableMissing=status==-6;
    Error=Ready?null:StatusText(status);
    Apply();
    return Ready;
   }catch(FileNotFoundException){Error="Aguardando canal de áudio";CloseMap();return false;}
    catch(UnauthorizedAccessException){Error="Sem acesso ao canal de áudio";CloseMap();return false;}
    catch(IOException){Error="Canal de áudio indisponível";CloseMap();return false;}
  }

  void Apply(){
   if(view==null)return;
   try{view.Write(20,volumePermille);view.Write(24,requestedActive?1:0);view.Write(48,micGainPercent);view.Write(52,denoise);view.Write(56,gatePercent);}
   catch(ObjectDisposedException){CloseMap();}
   catch(IOException){CloseMap();}
  }

  static string StatusText(int status){
   switch(status){case 1:return "Iniciando canal de áudio";case 3:return "Canal de áudio parado";case -1:return "Processo do RoxyPad não encontrado";case -2:return "Windows não ativou a captura do aplicativo";case -3:return "Falha ao abrir a captura do aplicativo";case -4:return "Formato de áudio incompatível";case -5:return "Falha na captura do áudio";case -6:return "VB-CABLE não instalado";case -7:return "Nenhum microfone encontrado";default:return "Aguardando canal de áudio";}
  }

  void CloseMap(){if(view!=null){view.Dispose();view=null;}if(mapping!=null){mapping.Dispose();mapping=null;}}

  public void Dispose(){
   if(disposed)return;
   requestedActive=false;Apply();disposed=true;CloseMap();
   if(process!=null){
    try{if(!process.HasExited){if(!process.WaitForExit(700))process.Kill();}}
    catch(InvalidOperationException){}
    catch(System.ComponentModel.Win32Exception){}
    finally{process.Dispose();process=null;}
   }
  }
 }
}
