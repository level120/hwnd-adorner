using System;
using System.Collections;

namespace HwndExtensions.Utils;

/// <summary>
/// Single Child Enumerator
/// </summary>
public class SingleChildEnumerator : IEnumerator
{
    private readonly object? _mChild;
    private State _mState;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChildEnumerator"/> class.
    /// </summary>
    /// <param name="child">child</param>
    public SingleChildEnumerator(object? child)
    {
        _mChild = child;
    }

    private enum State
    {
        Reset,
        Current,
        Finished,
    }

    /// <summary>
    /// Gets Current
    /// </summary>
    public object? Current
    {
        get
        {
            if (_mState != State.Current)
            {
                throw new InvalidOperationException();
            }

            return _mChild;
        }
    }

    /// <summary>
    /// Reset
    /// </summary>
    public void Reset()
    {
        _mState = State.Reset;
    }

    /// <summary>
    /// Move Next
    /// </summary>
    /// <returns>Result</returns>
    public bool MoveNext()
    {
        switch (_mState)
        {
            case State.Reset:
                _mState = State.Current;
                return true;

            case State.Current:
                _mState = State.Finished;
                return false;

            case State.Finished:
                return false;

            default:
                return false;
        }
    }
}