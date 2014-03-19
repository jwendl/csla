﻿﻿//-----------------------------------------------------------------------
// <copyright file="Binding.cs" company="Marimer LLC">
//     Copyright (c) Marimer LLC. All rights reserved.
//     Website: http://www.lhotka.net/cslanet/
// </copyright>
// <summary>Contains an individual binding to tie a property on an Axml view to the property on a supplied object</summary>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using MonoTouch.UIKit;

namespace Csla.Iosui.Binding
{
  /// <summary>
  /// Contains an individual binding to tie a property on an Axml view to the property on a supplied object.
  /// </summary>
  public class Binding : IDisposable
  {    
    /// <summary>
    /// Indicates if this is read only or two way binding. 
    /// </summary>
    public BindingDirection BindingDirection { get; private set; }  
    
    /// <summary>
    /// The iOS view that is used by the binding.
    /// </summary>
    public UIView Target { get; private set; }

    /// <summary>
    /// The object that the view is bound to.
    /// </summary>
    public object Source { get; private set; }

    /// <summary>
    /// The PropertyInfo for the property on the target view that is being bound to.
    /// </summary>
    public System.Reflection.PropertyInfo TargetProperty { get; private set; }

    /// <summary>
    /// The PropertyInfo for the property on the source object that is being bound to.
    /// </summary>
    public System.Reflection.PropertyInfo SourceProperty { get; private set; }

    /// <summary>
    /// A reference to a function to do a custom conversion from the source property to the target property.
    /// </summary>
    public Func<object, object> Convert { get; private set; }

    /// <summary>
    /// A reference to a function to do a custom conversion from the target property to the source property.
    /// </summary>
    public Func<object, object> ConvertBack { get; private set; }

    /// <summary>
    /// Creates a new instance of the Binding class.
    /// </summary>
    /// <param name="target">A reference to the control to bind to.</param>
    /// <param name="targetProperty">The name of the property on the view to bind to.</param>
    /// <param name="source">A reference to the object that will be bound to the control.</param>
    /// <param name="sourceProperty">The name of the property on the object that will be bound to the target property.</param>
    /// <param name="bindingDirection">Indicates if the binding is one way or two way.</param>
    public Binding(UIView target, string targetProperty, object source, string sourceProperty, BindingDirection bindingDirection)
        : this(target, targetProperty, source, sourceProperty, null, null, bindingDirection)
    { }

    /// <summary>
    /// Creates a new instance of the Binding class.
    /// </summary>
    /// <param name="target">A reference to the control to bind to.</param>
    /// <param name="targetProperty">The name of the property on the view to bind to.</param>
    /// <param name="source">A reference to the object that will be bound to the control.</param>
    /// <param name="sourceProperty">The name of the property on the object that will be bound to the target property.</param>
    /// <param name="convert">A reference to a function to do a custom conversion from the source property to the target property.</param>
    /// <param name="bindingDirection">Indicates if the binding is one way or two way.</param>
    public Binding(UIView target, string targetProperty, object source, string sourceProperty, Func<object, object> convert, BindingDirection bindingDirection)
        : this(target, targetProperty, source, sourceProperty, convert, null, bindingDirection)
    { }

    /// <summary>
    /// Creates a new instance of the Binding class.
    /// </summary>
    /// <param name="target">A reference to the control to bind to.</param>
    /// <param name="targetProperty">The name of the property on the view to bind to.</param>
    /// <param name="source">A reference to the object that will be bound to the control.</param>
    /// <param name="sourceProperty">The name of the property on the object that will be bound to the target property.</param>
    /// <param name="convert">A reference to a function to do a custom conversion from the source property to the target property.</param>
    /// <param name="convertBack">A reference to a function to do a custom conversion from the target property to the source property.</param>
    /// <param name="bindingDirection">Indicates if the binding is one way or two way.</param>
    public Binding(UIView target, string targetProperty, object source, string sourceProperty, Func<object, object> convert, Func<object, object> convertBack, BindingDirection bindingDirection)
    {
      BindingDirection = bindingDirection;
      Target = target;
      Source = source;
      TargetProperty = Target.GetType().GetProperty(targetProperty);
      SourceProperty = Source.GetType().GetProperty(sourceProperty);
      Convert = convert;
      ConvertBack = convertBack;

      var editableControl = Target as UIControl;
      if (editableControl != null)
        editableControl.EditingDidEnd += Target_EditingDidEnd;

      var inpc = Source as INotifyPropertyChanged;
      if (inpc != null)
        inpc.PropertyChanged += Source_PropertyChanged;

      UpdateTarget();
    }

    /// <summary>
    /// Event handler that is fired when the source property change event happens and the target needs to be updated.
    /// </summary>
    /// <param name="sender">A reference to the object that caused the event.</param>
    /// <param name="e">Event arguments.</param>
    void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      UpdateTarget();
    }

    /// <summary>
    /// Event handler that is fired when the target property change event happens and the source needs to be updated.
    /// </summary>
    /// <param name="sender">A reference to the object that caused the event.</param>
    /// <param name="e">Event arguments.</param>
    void Target_EditingDidEnd(object sender, EventArgs e)
    {
      var control = sender as UIControl;
      if (control != null && !control.Selected)
      {
        UpdateSource();
      }
    }

    /// <summary>
    /// Updates the source with the current value in the target object.  Uses the ConvertBack function to convert the data if available.
    /// </summary>
    public void UpdateSource()
    {
      if (this.BindingDirection == BindingDirection.OneWay)
        return;
      var value = TargetProperty.GetValue(Target, null);
      if (ConvertBack != null)
        value = ConvertBack(value);
      var converted = Utilities.CoerceValue(
        SourceProperty.PropertyType, TargetProperty.PropertyType, null, value);
      SourceProperty.SetValue(Source, 
        converted, 
        null);
    }

    /// <summary>
    /// Updates the target with the current value in the source object.  Uses the Convert function to convert the data if available.
    /// </summary>
    public void UpdateTarget()
    {
      var value = SourceProperty.GetValue(Source, null);
      if (Convert != null)
        value = Convert(value);
      var converted = Utilities.CoerceValue(
        TargetProperty.PropertyType, SourceProperty.PropertyType, null, value);
      TargetProperty.SetValue(Target,
        converted,
        null);
    }

    /// <summary>
    /// Clears the bindings, references and event handlers.
    /// </summary>
    public void Dispose()
    {
      var editableControl = Target as UIControl;
      if (editableControl != null)
          editableControl.EditingDidEnd -= Target_EditingDidEnd;

      var inpc = Source as INotifyPropertyChanged;
      if (inpc != null)
        inpc.PropertyChanged -= Source_PropertyChanged;
      Target = null;
      Source = null;
      TargetProperty = null;
      SourceProperty = null;
    }
  }
}