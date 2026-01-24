from fibonacci_generator.main import generate_fibonacci

test_cases = [-5, 0, 1, 7, 15]

print("=== Automated Verification ===")
for n in test_cases:
    print(f"Input: {n}")
    try:
        result = generate_fibonacci(n)
        print(f"Output: {result}")
    except Exception as e:
        print(f"Error: {e}")
    print("-" * 20)
