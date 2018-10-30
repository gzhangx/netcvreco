import React, { Component } from 'react';
import './App.css';
import DriveArea from './DriveArea';

class App extends Component {
  render() {
    return (
      <div className="App">
        <header className="App-header">
          <DriveArea/>
        </header>
      </div>
    );
  }
}

export default App;
