using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class RegisterMap
{
    // Arduino Register Map
    public const int COMMAND_LED = 0x02;
    public const int COMMAND_MODE = 0x04;
    public const int COMMAND_MOTOR = 0x05;
    public const int COMMAND_MOTOR_CONFIRM = 0x06;
    public const int COMMAND_START = 0x08;
    public const int COMMAND_STOP = 0x09;
    public const int PARAMETER_P = 0x10;
    public const int PARAMETER_I = 0x11;
    public const int PARAMETER_D = 0x12;

    // PC Register Map
    public const int MOTOR_DEGREE_1 = 0x09;
    public const int MOTOR_SPEED_1 =  0x11;
    public const int MOTOR_DEGREE_2 =   0x12;
    public const int MOTOR_SPEED_2 =    0x13;
    public const int START_REPLY =      0x14;
    public const int CURRENT_COMMAND =  0x15;
    public const int CURRENT_TIME =     0x20;
}
