using System;
using System.IO;
using System.Linq;
namespace RoxyPad {
 static class Tests {
  static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  public static void Run(string parent){
   string root=Path.Combine(parent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
   try{
    string source=Path.Combine(root,"áudio de teste.wav");File.WriteAllBytes(source,new byte[]{1,2,3,4});
    var lib=new Library(Path.Combine(root,"library"));var a=lib.Import(source);var b=lib.Import(source);
    Check(a.FileName!=b.FileName,"Unique owned filenames");Check(File.ReadAllBytes(lib.AudioPath(a)).SequenceEqual(File.ReadAllBytes(source)),"Import copies exact bytes");
    lib.Rename(a,"Risada Ágil");lib.ToggleFavorite(a);var reopen=new Library(lib.Root);
    Check(reopen.Clips.Count==2&&reopen.Clips[0].Favorite&&reopen.Clips[0].Name=="Risada Ágil","Persisted reopen");
    Check(reopen.Find("RISADA",true).Count()==1,"Search and favorites");
    var outdir=Path.Combine(root,"downloads");var one=lib.Export(a,outdir);var two=lib.Export(a,outdir);Check(one!=two&&File.ReadAllBytes(one).SequenceEqual(File.ReadAllBytes(source)),"Export collision preserves bytes");
    lib.Rename(a,"CON");Check(Path.GetFileName(lib.Export(a,outdir))=="_CON.wav","Reserved filename");
    lib.Remove(a);Check(File.Exists(source)&&new Library(lib.Root).Clips.Count==1,"Remove preserves original");
    bool rejected=false;try{lib.Import(Path.Combine(root,"file.exe"));}catch(InvalidDataException){rejected=true;}Check(rejected,"Reject unsupported extension");
    var audio=new LocalAudio();foreach(var v in new[]{0.0,0.13,0.5,1.0}){audio.Volume=v;for(int cycle=0;cycle<3;cycle++){audio.Close();Check(Math.Abs(audio.AppliedVolume-v)<0.00001,"Actual MediaPlayer volume preserved after Close");Check(audio.AppliedMute==(v==0),"Mute preserved after Close");}}
    File.WriteAllText(Path.Combine(parent,"result.txt"),"PASS: import bytes, duplicate names, persisted reopen, rename, favorites, search, export collision, reserved filename, remove preservation, unsupported extension.\nPASS: volume and mute retained in actual MediaPlayer across 12 Close cycles (0%, 13%, 50%, 100%).\nFixtures: "+root);
   }catch(Exception ex){File.WriteAllText(Path.Combine(parent,"result.txt"),"FAIL: "+ex);Environment.ExitCode=1;}
  }
 }
}
