import React, { Component } from 'react';
import './App.css';
import api from './api';

class DriveArea extends Component{
    constructor(props) {
        super(props);

        this.state = { x: 0, y: 0 };
    }

    _onMouseMove(e) {

        //200->180
        //const boundingRect = this.refs.drawArea.getBoundingClientRect(); //left top
        //e.screenX - boundingRect.left, y: e.screenY - boundingRect.top
        let x = parseInt((e.nativeEvent.offsetX -5)/10)*10;
        let y = 100 - e.nativeEvent.offsetY;
        if (y < 0) y = 0;
        y = parseInt(y/20);
        this.setState({ x, y });

        api.cmdReq(`r/${x}`);
        api.cmdReq(`d/${y}`);
    }

    render() {
        const { x, y } = this.state;
        return <div ref="drawArea" onMouseMove={this._onMouseMove.bind(this)} className="App-work-area">
            <p>Mouse coordinates: { x },{ y }</p>
            <svg>
                <line
                    x1={100}
                    y1={200}
                    x2={x}
                    y2={y}
                    style={{
                        stroke: '#458232',
                        strokeWidth: '3px',
                    }}
                ></line>
            </svg>
        </div>;
    }
}
export default DriveArea;