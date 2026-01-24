def generate_fibonacci(n):
    """
    Generates a list containing the Fibonacci sequence up to n terms.
    """
    if n <= 0:
        return []
    elif n == 1:
        return [0]
    elif n == 2:
        return [0, 1]

    fib_sequence = [0, 1]
    while len(fib_sequence) < n:
        next_val = fib_sequence[-1] + fib_sequence[-2]
        fib_sequence.append(next_val)
    return fib_sequence

def main():
    print("=== Fibonacci Generator ===")
    try:
        user_input = input("Enter the number of terms: ")
        if not user_input.strip(): # Handle empty input gracefully
             print("Please enter a valid number.")
             return
        n = int(user_input)
        
        if n < 0:
             print("Please enter a non-negative integer.")
        else:
             result = generate_fibonacci(n)
             print(f"Fibonacci sequence ({n} terms):")
             print(result)
    except ValueError:
        print("Invalid input. Please enter an integer.")

if __name__ == "__main__":
    main()
