__kernel void generate_passwords(__global char* output, __global const char* charset, const uint charset_length, const uint max_length, const ulong total_combinations) {
    ulong global_id = get_global_id(0);

    if (global_id >= total_combinations) {
        return; // Skip if the global ID exceeds the total number of combinations
    }

    ulong temp_id = global_id;
    uint idx = 0;

    // Initialize output for this ID
    for (uint i = 0; i < max_length; i++) {
        output[global_id * max_length + i] = 0;
    }

    // Generate password combination
    while (temp_id > 0 && idx < max_length) {
        uint char_idx = temp_id % charset_length;
        output[global_id * max_length + idx] = charset[char_idx];
        temp_id /= charset_length;
        idx++;
    }
}
