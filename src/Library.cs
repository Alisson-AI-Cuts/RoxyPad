using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace RoxyPad {
 public class Clip {
  public string Id { get; set; }
  public string Name { get; set; }
  public string FileName { get; set; }
  public bool Favorite { get; set; }
 }
 public class Library {
  public readonly string Root;
  public List<Clip> Clips = new List<Clip>();
  static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<Clip>));
  public Library(string root) {
   Root = root; Directory.CreateDirectory(Path.Combine(root,"audio"));
   if(File.Exists(Index)) using(var s=File.OpenRead(Index)) Clips=(List<Clip>)Serializer.Deserialize(s);
   foreach(var c in Clips) if(string.IsNullOrEmpty(c.FileName) || Path.GetFileName(c.FileName)!=c.FileName) throw new InvalidDataException("Caminho inválido na biblioteca.");
  }
  string Index {get{return Path.Combine(Root,"library.xml");}}
  public string AudioPath(Clip c){return Path.Combine(Root,"audio",c.FileName);}
  public void Save(){
   var temp=Index+".tmp";
   using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {Serializer.Serialize(s,Clips);s.Flush(true);}
   if(File.Exists(Index)) File.Replace(temp,Index,Index+".bak"); else File.Move(temp,Index);
  }
  public Clip Import(string source){
   string ext=Path.GetExtension(source).ToLowerInvariant();
   if(!new[]{".mp3",".wav",".m4a",".aac",".wma"}.Contains(ext)) throw new InvalidDataException("Formato não aceito: "+ext);
   var c=new Clip{Id=Guid.NewGuid().ToString("N"),Name=Path.GetFileNameWithoutExtension(source)};
   c.FileName=c.Id+ext; File.Copy(source,AudioPath(c),false); Clips.Add(c);
   try{Save();} catch {Clips.Remove(c);File.Delete(AudioPath(c));throw;} return c;
  }
  public void Rename(Clip c,string name){name=name.Trim();if(name.Length==0 || name.Length>120) throw new ArgumentException("Use um nome de 1 a 120 caracteres."); var old=c.Name;c.Name=name;try{Save();}catch{c.Name=old;throw;}}
  public void ToggleFavorite(Clip c){c.Favorite=!c.Favorite;try{Save();}catch{c.Favorite=!c.Favorite;throw;}}
  public void Remove(Clip c){int i=Clips.IndexOf(c);Clips.Remove(c);try{Save();}catch{Clips.Insert(i,c);throw;} /* Retain owned audio for recovery; never delete original. */ }
  public string Export(Clip c,string directory){
   Directory.CreateDirectory(directory);string name=c.Name;
   foreach(char x in Path.GetInvalidFileNameChars())name=name.Replace(x,'_');
   name=name.Trim().TrimEnd('.');if(name.Length==0)name="audio";
   var reserved=new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"};
   if(reserved.Contains(name.Split('.')[0].ToUpperInvariant()))name="_"+name;
   var ext=Path.GetExtension(c.FileName);int n=0;
   while(true){var p=Path.Combine(directory,name+(n==0?"":" ("+n+")")+ext);try{File.Copy(AudioPath(c),p,false);return p;}catch(IOException){if(!File.Exists(p))throw;n++;}}
  }
  public IEnumerable<Clip> Find(string query,bool favorites){return Clips.Where(c=>(!favorites||c.Favorite)&&c.Name.IndexOf(query.Trim(),StringComparison.CurrentCultureIgnoreCase)>=0);}
 }
}
