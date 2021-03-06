//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.5
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace SWIG.BWAPI {

public partial class BulletType : Type {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal BulletType(global::System.IntPtr cPtr, bool cMemoryOwn) : base(bwapiPINVOKE.BulletType_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(BulletType obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~BulletType() {
    Dispose();
  }

  public override void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          bwapiPINVOKE.delete_BulletType(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
      base.Dispose();
    }
  }

  
public override int GetHashCode()
{
   return this.swigCPtr.Handle.GetHashCode();
}

public override bool Equals(object obj)
{
    bool equal = false;
    if (obj is BulletType)
      equal = (((BulletType)obj).swigCPtr.Handle == this.swigCPtr.Handle);
    return equal;
}
  
public bool Equals(BulletType obj) 
{
    if (obj == null) return false;
    return (obj.swigCPtr.Handle == this.swigCPtr.Handle);
}

public static bool operator ==(BulletType obj1, BulletType obj2)
{
    if (object.ReferenceEquals(obj1, obj2)) return true;
    if (object.ReferenceEquals(obj1, null)) return false;
    if (object.ReferenceEquals(obj2, null)) return false;
   
    return obj1.Equals(obj2);
}

public static bool operator !=(BulletType obj1, BulletType obj2)
{
    if (object.ReferenceEquals(obj1, obj2)) return false;
    if (object.ReferenceEquals(obj1, null)) return true;
    if (object.ReferenceEquals(obj2, null)) return true;

    return !obj1.Equals(obj2);
}




  public BulletType() : this(bwapiPINVOKE.new_BulletType__SWIG_0(), true) {
  }

  public BulletType(int id) : this(bwapiPINVOKE.new_BulletType__SWIG_1(id), true) {
  }

  public string getName() {
    string ret = bwapiPINVOKE.BulletType_getName(swigCPtr);
    return ret;
  }

  public string c_str() {
    string ret = bwapiPINVOKE.BulletType_c_str(swigCPtr);
    return ret;
  }

}

}
