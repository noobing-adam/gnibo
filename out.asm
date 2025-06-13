global _start
_start:
    mov rax, 2
    push rax
    mov rax, 3
    push rax
    mov rax, 2
    push rax
    pop rax
    pop rbx
    mul rbx
    push rax
    mov rax, 10
    push rax
    pop rax
    pop rbx
    sub rax, rbx
    push rax
    pop rax
    pop rbx
    div rbx
    push rax
    mov rax, 1
    push rax
    mov rax, 0
    push rax
    pop rax
    test rax, rax
    jz label0
    mov rax, 0xA3936
    push rax
    mov rax, 1
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 3
    syscall
    add rsp, 8
    jnz label1
label0:
    mov rax, 0
    push rax
    pop rax
    test rax, rax
    jz label2
    mov rax, 0xA3636
    push rax
    mov rax, 1
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 3
    syscall
    add rsp, 8
    jmp label1
label2:
    mov rax, 0
    push rax
    pop rax
    test rax, rax
    jz label3
    mov rax, 0xA3535
    push rax
    mov rax, 1
    mov rdi, 1
    mov rsi, rsp
    mov rdx, 3
    syscall
    add rsp, 8
    jmp label1
label3:
    mov rax, 233
    push rax
    mov rax, 60
    pop rdi
    syscall
label1:
    mov rax, 60
    mov rdi, 0
    syscall